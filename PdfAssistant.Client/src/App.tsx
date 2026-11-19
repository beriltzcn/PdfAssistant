import {
  useEffect,
  useRef,
  useState,
  type ChangeEvent,
  type FormEvent,
} from 'react'
import './App.css'
import {
  askDocument,
  deleteDocument,
  getApiErrorMessage,
  getDocuments,
  processDocument,
  uploadDocument,
  type DocumentSummary,
} from './api/documents'

const maxFileSize = 10 * 1024 * 1024

type ApiStatus = 'connecting' | 'connected' | 'disconnected'

interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
}

const suggestedQuestions = [
  'Summarize this document.',
  'What are the key points?',
  'List the important terms.',
]

function formatDocumentDate(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return 'Unknown date'
  }

  const today = new Date()
  const startOfToday = new Date(
    today.getFullYear(),
    today.getMonth(),
    today.getDate(),
  )
  const startOfDocumentDay = new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
  )
  const dayDifference = Math.round(
    (startOfToday.getTime() - startOfDocumentDay.getTime()) /
      (24 * 60 * 60 * 1000),
  )

  if (dayDifference === 0) {
    return 'Today'
  }

  if (dayDifference === 1) {
    return 'Yesterday'
  }

  return new Intl.DateTimeFormat('en', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(date)
}

function formatFileSize(bytes: number) {
  if (bytes < 1024 * 1024) {
    return `${Math.max(1, Math.round(bytes / 1024))} KB`
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function getStatusLabel(status: string) {
  const labels: Record<string, string> = {
    processing: 'Processing',
    ready: 'Ready',
    failed: 'Failed',
  }

  return labels[status.toLowerCase()] ?? status
}

function App() {
  const [documents, setDocuments] = useState<DocumentSummary[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isUploading, setIsUploading] = useState(false)
  const [deletingDocumentId, setDeletingDocumentId] = useState<number | null>(
    null,
  )
  const [retryingDocumentId, setRetryingDocumentId] = useState<number | null>(
    null,
  )
  const [confirmingDeleteId, setConfirmingDeleteId] = useState<number | null>(
    null,
  )
  const [apiStatus, setApiStatus] = useState<ApiStatus>('connecting')
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [selectedDocument, setSelectedDocument] =
    useState<DocumentSummary | null>(null)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [question, setQuestion] = useState('')
  const [isAsking, setIsAsking] = useState(false)
  const [chatError, setChatError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const chatEndRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    let ignore = false

    getDocuments()
      .then((items) => {
        if (!ignore) {
          setDocuments(items)
          setApiStatus('connected')
          setError(null)
        }
      })
      .catch((requestError: unknown) => {
        if (!ignore) {
          setApiStatus('disconnected')
          setError(getApiErrorMessage(requestError))
        }
      })
      .finally(() => {
        if (!ignore) {
          setIsLoading(false)
        }
      })

    return () => {
      ignore = true
    }
  }, [])

  useEffect(() => {
    if (selectedDocument) {
      chatEndRef.current?.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest',
      })
    }
  }, [messages, isAsking, selectedDocument])

  const totalPages = documents.reduce(
    (total, document) => total + document.pageCount,
    0,
  )

  async function refreshDocuments() {
    const items = await getDocuments()
    setDocuments(items)
    setApiStatus('connected')
  }

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''

    if (!file) {
      return
    }

    if (!file.name.toLowerCase().endsWith('.pdf')) {
      setError('Only PDF files can be uploaded.')
      setNotice(null)
      return
    }

    if (file.size > maxFileSize) {
      setError('The PDF file cannot exceed 10 MB.')
      setNotice(null)
      return
    }

    setIsUploading(true)
    setError(null)
    setNotice(null)
    let uploadSucceeded = false

    try {
      const response = await uploadDocument(file)
      uploadSucceeded = true

      setDocuments((currentDocuments) => [
        response.document,
        ...currentDocuments.filter(
          (document) => document.id !== response.document.id,
        ),
      ])
      setApiStatus('connected')
      setNotice(
        `${response.document.originalFileName} uploaded. Preparing it for chat…`,
      )

      const processedDocument = await processDocument(
        response.document.storedFileName,
      )

      setDocuments((currentDocuments) =>
        currentDocuments.map((document) =>
          document.id === processedDocument.id
            ? processedDocument
            : document,
        ),
      )
      setNotice(
        `${processedDocument.originalFileName} is ready for chat.`,
      )

      try {
        await refreshDocuments()
      } catch {
        // The optimistic update keeps the uploaded document visible.
      }
    } catch (requestError: unknown) {
      const message = getApiErrorMessage(requestError)

      setError(
        uploadSucceeded
          ? `The PDF was uploaded, but preparation failed. ${message}`
          : message,
      )

      if (uploadSucceeded) {
        try {
          await refreshDocuments()
        } catch {
          // Keep the uploaded document visible with its optimistic state.
        }
      }
    } finally {
      setIsUploading(false)
    }
  }

  async function handleRetry() {
    setIsLoading(true)
    setError(null)

    try {
      await refreshDocuments()
    } catch (requestError: unknown) {
      setApiStatus('disconnected')
      setError(getApiErrorMessage(requestError))
    } finally {
      setIsLoading(false)
    }
  }

  function openChat(document: DocumentSummary) {
    if (document.status !== 'ready') {
      return
    }

    setSelectedDocument(document)
    setMessages([])
    setQuestion('')
    setChatError(null)
    setError(null)
    setNotice(null)
  }

  function closeChat() {
    if (isAsking) {
      return
    }

    setSelectedDocument(null)
    setMessages([])
    setQuestion('')
    setChatError(null)
  }

  async function handleAsk(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!selectedDocument || isAsking) {
      return
    }

    const currentQuestion = question.trim()

    if (!currentQuestion) {
      return
    }

    setQuestion('')
    setChatError(null)
    setIsAsking(true)
    setMessages((currentMessages) => [
      ...currentMessages,
      {
        id: crypto.randomUUID(),
        role: 'user',
        content: currentQuestion,
      },
    ])

    try {
      const response = await askDocument(
        selectedDocument.storedFileName,
        currentQuestion,
      )

      setMessages((currentMessages) => [
        ...currentMessages,
        {
          id: crypto.randomUUID(),
          role: 'assistant',
          content: response.answer,
        },
      ])
    } catch (requestError: unknown) {
      setChatError(getApiErrorMessage(requestError))
    } finally {
      setIsAsking(false)
    }
  }

  async function handleReprocess(document: DocumentSummary) {
    setRetryingDocumentId(document.id)
    setError(null)
    setNotice(`Reprocessing ${document.originalFileName}…`)

    try {
      const processedDocument = await processDocument(
        document.storedFileName,
      )

      setDocuments((currentDocuments) =>
        currentDocuments.map((currentDocument) =>
          currentDocument.id === processedDocument.id
            ? processedDocument
            : currentDocument,
        ),
      )
      setNotice(
        `${processedDocument.originalFileName} is ready for chat.`,
      )

      try {
        await refreshDocuments()
      } catch {
        // Keep the updated document visible with its optimistic state.
      }
    } catch (requestError: unknown) {
      setError(getApiErrorMessage(requestError))

      try {
        await refreshDocuments()
      } catch {
        // Keep the existing row available for another retry.
      }
    } finally {
      setRetryingDocumentId(null)
    }
  }

  async function handleDelete(document: DocumentSummary) {
    setDeletingDocumentId(document.id)
    setError(null)

    try {
      await deleteDocument(document.storedFileName)
      setDocuments((currentDocuments) =>
        currentDocuments.filter(
          (currentDocument) => currentDocument.id !== document.id,
        ),
      )
      setNotice(`${document.originalFileName} deleted.`)
      setConfirmingDeleteId(null)
    } catch (requestError: unknown) {
      setError(getApiErrorMessage(requestError))
      setConfirmingDeleteId(null)
    } finally {
      setDeletingDocumentId(null)
    }
  }

  return (
    <div className="app-shell">
      <header className="site-header">
        <div className="header-inner">
          <a className="brand" href="#top" aria-label="PdfAssistant home">
            <span className="brand-mark" aria-hidden="true">
              P
            </span>
            <span>PdfAssistant</span>
          </a>

          <nav className="main-nav" aria-label="Main navigation">
            <a className="active" href="#documents" aria-current="page">
              Documents
            </a>
          </nav>

          <div className="header-actions">
            <div className="api-state">
              <span
                className={`status-dot ${apiStatus}`}
                aria-hidden="true"
              />
              <span>
                {apiStatus === 'connected' && 'Connected'}
                {apiStatus === 'connecting' && 'Connecting'}
                {apiStatus === 'disconnected' && 'Disconnected'}
              </span>
            </div>

          </div>
        </div>
      </header>

      <main className="page" id="top">
        <section className="page-header">
          <div>
            <p className="eyebrow">
              {selectedDocument ? 'PDF CHAT' : 'PERSONAL LIBRARY'}
            </p>
            <h1>
              {selectedDocument ? 'Ask your document' : 'Documents'}
            </h1>
            <p className="page-description">
              {selectedDocument
                ? selectedDocument.originalFileName
                : 'Upload your PDF files and keep them in one place.'}
            </p>
          </div>

          {!selectedDocument && (
            <div className="upload-controls">
              <button
                className="primary-button"
                type="button"
                onClick={() => fileInputRef.current?.click()}
                disabled={isUploading}
              >
                {isUploading ? 'Uploading…' : '+ Upload PDF'}
              </button>
              <span>Up to 10 MB</span>
            </div>
          )}

          <input
            ref={fileInputRef}
            className="file-input"
            type="file"
            accept=".pdf,application/pdf"
            onChange={handleFileChange}
            tabIndex={-1}
            aria-label="Choose a PDF file to upload"
          />
        </section>

        <section
          className="summary-bar"
          aria-label="Document summary"
          hidden={Boolean(selectedDocument)}
        >
          <div>
            <strong>{documents.length}</strong>
            <span>Documents</span>
          </div>
          <div>
            <strong>{totalPages}</strong>
            <span>Pages</span>
          </div>
          <div>
            <strong>
              {
                documents.filter(
                  (document) => document.status === 'ready',
                ).length
              }
            </strong>
            <span>Processed</span>
          </div>
        </section>

        {!selectedDocument && error && (
          <div className="feedback-message error-message" role="alert">
            <span>{error}</span>
            <button
              type="button"
              onClick={() => {
                if (apiStatus === 'disconnected') {
                  void handleRetry()
                  return
                }

                setError(null)
              }}
            >
              {apiStatus === 'disconnected' ? 'Try again' : 'Dismiss'}
            </button>
          </div>
        )}

        {!selectedDocument && notice && (
          <div className="feedback-message success-message" role="status">
            <span>{notice}</span>
          </div>
        )}

        <section
          className="documents-section"
          id="documents"
          aria-labelledby="documents-title"
          hidden={Boolean(selectedDocument)}
        >
          <div className="section-heading">
            <div>
              <h2 id="documents-title">All documents</h2>
              <p>Your uploaded PDF files appear here.</p>
            </div>

            {!isLoading && documents.length > 0 && (
              <span className="file-count">
                {documents.length} {documents.length === 1 ? 'file' : 'files'}
              </span>
            )}
          </div>

          {isLoading ? (
            <div className="document-state" role="status">
              <span className="loading-spinner" aria-hidden="true" />
              Loading documents…
            </div>
          ) : documents.length > 0 ? (
            <div className="document-list" role="list">
              {documents.map((document) => {
                const isDeleting = deletingDocumentId === document.id
                const isRetrying = retryingDocumentId === document.id
                const isBusy = isDeleting || isRetrying

                return (
                  <article
                    className="document-row"
                    key={document.id}
                    role="listitem"
                  >
                    <div className="file-type" aria-hidden="true">
                      PDF
                    </div>

                    <div className="document-copy">
                      <h3 title={document.originalFileName}>
                        {document.originalFileName}
                      </h3>
                      <p>
                        {formatDocumentDate(document.createdAtUtc)}
                        <span aria-hidden="true"> · </span>
                        {formatFileSize(document.sizeInBytes)}
                        <span aria-hidden="true"> · </span>
                        {document.pageCount > 0
                          ? `${document.pageCount} pages`
                          : 'Page count pending'}
                      </p>
                    </div>

                    <span className="document-status">
                      {getStatusLabel(document.status)}
                    </span>

                    <div className="document-actions">
                      {confirmingDeleteId === document.id ? (
                        <>
                          <span className="delete-confirm-label">
                            Delete this PDF?
                          </span>
                          <button
                            className="document-action danger"
                            type="button"
                            onClick={() => void handleDelete(document)}
                            disabled={isDeleting}
                          >
                            {isDeleting ? 'Deleting…' : 'Confirm'}
                          </button>
                          <button
                            className="document-action muted"
                            type="button"
                            onClick={() => setConfirmingDeleteId(null)}
                            disabled={isDeleting}
                          >
                            Cancel
                          </button>
                        </>
                      ) : (
                        <>
                          {document.status === 'ready' && (
                            <button
                              className="document-action"
                              type="button"
                              onClick={() => openChat(document)}
                              disabled={isBusy}
                            >
                              Chat
                            </button>
                          )}

                          {document.status === 'failed' && (
                            <button
                              className="document-action"
                              type="button"
                              onClick={() => void handleReprocess(document)}
                              disabled={isBusy}
                            >
                              {isRetrying ? 'Retrying…' : 'Retry'}
                            </button>
                          )}

                          <button
                            className="document-action danger"
                            type="button"
                            onClick={() =>
                              setConfirmingDeleteId(document.id)
                            }
                            disabled={isBusy}
                          >
                            Delete
                          </button>
                        </>
                      )}
                    </div>
                  </article>
                )
              })}
            </div>
          ) : (
            <div className="empty-state">
              <span className="empty-symbol" aria-hidden="true">
                +
              </span>
              <h3>No documents yet</h3>
              <p>Upload your first PDF to start your library.</p>
              <button
                className="text-button"
                type="button"
                onClick={() => fileInputRef.current?.click()}
                disabled={isUploading}
              >
                Choose PDF
              </button>
            </div>
          )}
        </section>

        {selectedDocument && (
          <section
            className="chat-section"
            aria-labelledby="chat-title"
          >
            <div className="chat-toolbar">
              <button
                className="back-button"
                type="button"
                onClick={closeChat}
                disabled={isAsking}
              >
                ← All documents
              </button>
              <span className="document-status">
                {getStatusLabel(selectedDocument.status)}
              </span>
            </div>

            <div className="chat-heading">
              <h2 id="chat-title">Ask this PDF</h2>
              <p>
                {selectedDocument.pageCount > 0
                  ? `${selectedDocument.pageCount} pages`
                  : 'Document ready'}
                <span aria-hidden="true"> · </span>
                Answers are based only on this document.
              </p>
            </div>

            <div className="chat-thread" aria-live="polite">
              {messages.length === 0 && !isAsking && (
                <div className="chat-empty">
                  <strong>What would you like to know?</strong>
                  <p>
                    Ask a question or start with one of the suggestions.
                  </p>
                  <div className="chat-suggestions">
                    {suggestedQuestions.map((suggestion) => (
                      <button
                        type="button"
                        key={suggestion}
                        onClick={() => setQuestion(suggestion)}
                      >
                        {suggestion}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {messages.map((message) => (
                <div
                  className={`chat-message ${message.role}`}
                  key={message.id}
                >
                  <span className="chat-author">
                    {message.role === 'user' ? 'You' : 'PdfAssistant'}
                  </span>
                  <p>{message.content}</p>
                </div>
              ))}

              {isAsking && (
                <div className="chat-thinking" role="status">
                  <span className="loading-spinner" aria-hidden="true" />
                  Searching the document…
                </div>
              )}

              <div ref={chatEndRef} />
            </div>

            {chatError && (
              <div className="feedback-message error-message" role="alert">
                <span>{chatError}</span>
                <button type="button" onClick={() => setChatError(null)}>
                  Dismiss
                </button>
              </div>
            )}

            <form className="chat-composer" onSubmit={handleAsk}>
              <label htmlFor="document-question">Your question</label>
              <div className="chat-input-row">
                <input
                  id="document-question"
                  type="text"
                  value={question}
                  onChange={(event) => setQuestion(event.target.value)}
                  placeholder="Ask something about this PDF…"
                  autoComplete="off"
                  disabled={isAsking}
                />
                <button
                  className="primary-button"
                  type="submit"
                  disabled={isAsking || !question.trim()}
                >
                  {isAsking ? 'Asking…' : 'Ask'}
                </button>
              </div>
            </form>
          </section>
        )}

        <footer className="page-note">
          <strong>PdfAssistant</strong>
          <span>Your documents are stored only in this workspace.</span>
        </footer>
      </main>
    </div>
  )
}

export default App
