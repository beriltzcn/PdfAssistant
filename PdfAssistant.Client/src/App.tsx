import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type ChangeEvent,
  type FormEvent,
} from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
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
  const [selectedDocument, setSelectedDocument] =
    useState<DocumentSummary | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [question, setQuestion] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isUploading, setIsUploading] = useState(false)
  const [isAsking, setIsAsking] = useState(false)
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
  const [chatError, setChatError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const chatEndRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    let ignore = false

    getDocuments()
      .then((items) => {
        if (!ignore) {
          setDocuments(items)
          setSelectedDocument(findPreferredDocument(items))
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
  const readyCount = documents.filter(
    (document) => document.status === 'ready',
  ).length
  const filteredDocuments = useMemo(() => {
    const query = searchQuery.trim().toLowerCase()

    if (!query) {
      return documents
    }

    return documents.filter((document) =>
      document.originalFileName.toLowerCase().includes(query),
    )
  }, [documents, searchQuery])

  function resetConversation() {
    setMessages([])
    setQuestion('')
    setChatError(null)
  }

  function selectDocument(document: DocumentSummary) {
    if (isAsking || selectedDocument?.id === document.id) {
      return
    }

    setSelectedDocument(document)
    resetConversation()
    setError(null)
    setNotice(null)
    setConfirmingDeleteId(null)
  }

  async function refreshDocuments() {
    const items = await getDocuments()
    setDocuments(items)
    setSelectedDocument((currentDocument) => {
      if (currentDocument) {
        const updatedDocument = items.find(
          (document) => document.id === currentDocument.id,
        )

        if (updatedDocument) {
          return updatedDocument
        }
      }

      return findPreferredDocument(items)
    })
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
      setSelectedDocument(response.document)
      resetConversation()
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
      setSelectedDocument(processedDocument)
      setNotice(
        `${processedDocument.originalFileName} is ready for chat.`,
      )

      try {
        await refreshDocuments()
      } catch {
        // Keep the optimistic ready state visible.
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
          // Keep the uploaded document available for a retry.
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
      setSelectedDocument(processedDocument)
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
        // Keep the failed row available for another retry.
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

      const remainingDocuments = documents.filter(
        (currentDocument) => currentDocument.id !== document.id,
      )

      setDocuments(remainingDocuments)
      setNotice(`${document.originalFileName} deleted.`)
      setConfirmingDeleteId(null)

      if (selectedDocument?.id === document.id) {
        setSelectedDocument(findPreferredDocument(remainingDocuments))
        resetConversation()
      }
    } catch (requestError: unknown) {
      setError(getApiErrorMessage(requestError))
      setConfirmingDeleteId(null)
    } finally {
      setDeletingDocumentId(null)
    }
  }

  async function handleAsk(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (
      !selectedDocument ||
      selectedDocument.status !== 'ready' ||
      isAsking
    ) {
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

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">
            P
          </span>
          <strong>PdfAssistant</strong>
        </div>

        <nav className="main-nav" aria-label="Main navigation">
          <span className="active">Documents</span>
        </nav>

        <div className="header-status">
          <span
            className={`status-dot ${apiStatus}`}
            aria-hidden="true"
          />
          <span>
            {apiStatus === 'connected' && 'API connected'}
            {apiStatus === 'connecting' && 'Connecting'}
            {apiStatus === 'disconnected' && 'Disconnected'}
          </span>
        </div>
      </header>

      <div className="workspace">
        <aside className="library" aria-label="Document library">
          <div className="library-heading">
            <div>
              <p>LIBRARY</p>
              <h1>
                Documents <span>{documents.length}</span>
              </h1>
            </div>
            <button
              className="icon-button"
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={isUploading}
              aria-label="Upload PDF"
            >
              +
            </button>
          </div>

          <button
            className="upload-button"
            type="button"
            onClick={() => fileInputRef.current?.click()}
            disabled={isUploading}
          >
            {isUploading ? 'Preparing PDF…' : '+ Upload PDF'}
          </button>

          <label className="library-search">
            <span aria-hidden="true">⌕</span>
            <input
              type="search"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search documents"
            />
          </label>

          <div className="library-list">
            {isLoading ? (
              <div className="library-state">
                <span className="loading-spinner" aria-hidden="true" />
                Loading documents…
              </div>
            ) : filteredDocuments.length > 0 ? (
              filteredDocuments.map((document) => {
                const isSelected = selectedDocument?.id === document.id

                return (
                  <button
                    className={`library-document ${
                      isSelected ? 'selected' : ''
                    }`}
                    type="button"
                    key={document.id}
                    onClick={() => selectDocument(document)}
                    disabled={isAsking}
                    aria-pressed={isSelected}
                  >
                    <span className="file-icon" aria-hidden="true">
                      PDF
                    </span>
                    <span className="library-document-copy">
                      <strong title={document.originalFileName}>
                        {document.originalFileName}
                      </strong>
                      <small>
                        {document.pageCount > 0
                          ? `${document.pageCount} pages`
                          : 'Page count pending'}
                        <span aria-hidden="true"> · </span>
                        {formatDocumentDate(document.createdAtUtc)}
                      </small>
                    </span>
                    <span
                      className={`small-status ${document.status}`}
                    >
                      {getStatusLabel(document.status)}
                    </span>
                  </button>
                )
              })
            ) : (
              <div className="library-state">
                {searchQuery
                  ? 'No matching documents.'
                  : 'No documents yet.'}
              </div>
            )}
          </div>

          <div className="library-footer">
            <span>
              <strong>{readyCount}</strong> ready
            </span>
            <span>
              <strong>{totalPages}</strong> pages
            </span>
          </div>
        </aside>

        <main className="main-panel">
          {error && (
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

          {notice && (
            <div className="feedback-message success-message" role="status">
              <span>{notice}</span>
            </div>
          )}

          {selectedDocument ? (
            <section
              className="active-document"
              aria-labelledby="active-document-title"
            >
              <header className="document-header">
                <div className="document-heading">
                  <p>ACTIVE DOCUMENT</p>
                  <h2 id="active-document-title">
                    {selectedDocument.originalFileName}
                  </h2>
                  <span>
                    {selectedDocument.pageCount > 0
                      ? `${selectedDocument.pageCount} pages`
                      : 'Page count pending'}
                    <span aria-hidden="true"> · </span>
                    {formatFileSize(selectedDocument.sizeInBytes)}
                    <span aria-hidden="true"> · </span>
                    {getStatusLabel(selectedDocument.status)}
                  </span>
                </div>

                <div className="document-actions">
                  {confirmingDeleteId === selectedDocument.id ? (
                    <>
                      <span className="delete-prompt">Delete this PDF?</span>
                      <button
                        className="text-action danger"
                        type="button"
                        onClick={() => void handleDelete(selectedDocument)}
                        disabled={deletingDocumentId === selectedDocument.id}
                      >
                        {deletingDocumentId === selectedDocument.id
                          ? 'Deleting…'
                          : 'Confirm'}
                      </button>
                      <button
                        className="text-action"
                        type="button"
                        onClick={() => setConfirmingDeleteId(null)}
                        disabled={deletingDocumentId === selectedDocument.id}
                      >
                        Cancel
                      </button>
                    </>
                  ) : (
                    <>
                      {selectedDocument.status === 'failed' && (
                        <button
                          className="text-action"
                          type="button"
                          onClick={() =>
                            void handleReprocess(selectedDocument)
                          }
                          disabled={
                            retryingDocumentId === selectedDocument.id ||
                            isAsking
                          }
                        >
                          {retryingDocumentId === selectedDocument.id
                            ? 'Retrying…'
                            : 'Retry'}
                        </button>
                      )}
                      <button
                        className="text-action danger"
                        type="button"
                        onClick={() =>
                          setConfirmingDeleteId(selectedDocument.id)
                        }
                        disabled={
                          isAsking ||
                          retryingDocumentId === selectedDocument.id
                        }
                      >
                        Delete
                      </button>
                    </>
                  )}
                </div>
              </header>

              {selectedDocument.status === 'ready' ? (
                <>
                  <div className="chat-thread" aria-live="polite">
                    {messages.length === 0 && !isAsking && (
                      <div className="chat-empty">
                        <span className="assistant-mark" aria-hidden="true">
                          P
                        </span>
                        <div>
                          <h3>Ready when you are.</h3>
                          <p>
                            Ask a question and get an answer grounded in this
                            document.
                          </p>
                        </div>
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
                        <span>
                          {message.role === 'user'
                            ? 'You'
                            : 'PdfAssistant'}
                        </span>
                        <div
                          className={`message-bubble ${
                            message.role === 'assistant'
                              ? 'markdown-content'
                              : ''
                          }`}
                        >
                          {message.role === 'assistant' ? (
                            <ReactMarkdown
                              remarkPlugins={[remarkGfm]}
                            >
                              {message.content}
                            </ReactMarkdown>
                          ) : (
                            <p>{message.content}</p>
                          )}
                        </div>
                      </div>
                    ))}

                    {isAsking && (
                      <div className="chat-thinking" role="status">
                        <span
                          className="loading-spinner"
                          aria-hidden="true"
                        />
                        Searching the document…
                      </div>
                    )}

                    <div ref={chatEndRef} />
                  </div>

                  {chatError && (
                    <div
                      className="feedback-message error-message chat-error"
                      role="alert"
                    >
                      <span>{chatError}</span>
                      <button
                        type="button"
                        onClick={() => setChatError(null)}
                      >
                        Dismiss
                      </button>
                    </div>
                  )}

                  <form className="chat-composer" onSubmit={handleAsk}>
                    <label htmlFor="document-question">
                      Ask this document
                    </label>
                    <div className="composer-row">
                      <input
                        id="document-question"
                        type="text"
                        value={question}
                        onChange={(event) => setQuestion(event.target.value)}
                        placeholder="Type your question…"
                        autoComplete="off"
                        disabled={isAsking}
                      />
                      <button
                        className="send-button"
                        type="submit"
                        disabled={isAsking || !question.trim()}
                        aria-label="Ask question"
                      >
                        {isAsking ? 'Asking…' : 'Ask'}
                      </button>
                    </div>
                  </form>
                </>
              ) : (
                <div className="document-gate">
                  {selectedDocument.status === 'processing' ? (
                    <>
                      <span
                        className="loading-spinner"
                        aria-hidden="true"
                      />
                      <h3>Preparing this document</h3>
                      <p>Text and embeddings are being prepared for chat.</p>
                    </>
                  ) : (
                    <>
                      <span className="gate-symbol" aria-hidden="true">
                        !
                      </span>
                      <h3>Processing failed</h3>
                      <p>Retry to prepare this PDF for chat again.</p>
                      <button
                        className="primary-button"
                        type="button"
                        onClick={() => void handleReprocess(selectedDocument)}
                        disabled={
                          retryingDocumentId === selectedDocument.id
                        }
                      >
                        {retryingDocumentId === selectedDocument.id
                          ? 'Retrying…'
                          : 'Retry processing'}
                      </button>
                    </>
                  )}
                </div>
              )}
            </section>
          ) : (
            <div className="main-empty">
              <span className="empty-symbol" aria-hidden="true">
                PDF
              </span>
              <h2>
                {documents.length === 0
                  ? 'Your library is empty'
                  : 'Select a document'}
              </h2>
              <p>
                {documents.length === 0
                  ? 'Upload your first PDF to start a conversation.'
                  : 'Choose a PDF from the library to continue.'}
              </p>
              {documents.length === 0 && (
                <button
                  className="primary-button"
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  disabled={isUploading}
                >
                  Upload your first PDF
                </button>
              )}
            </div>
          )}
        </main>
      </div>

      <input
        ref={fileInputRef}
        className="file-input"
        type="file"
        accept=".pdf,application/pdf"
        onChange={handleFileChange}
        tabIndex={-1}
        aria-label="Choose a PDF file to upload"
      />
    </div>
  )
}

function findPreferredDocument(documents: DocumentSummary[]) {
  return (
    documents.find((document) => document.status === 'ready') ??
    documents[0] ??
    null
  )
}

export default App
