import axios from 'axios'

export interface DocumentSummary {
  id: number
  storedFileName: string
  originalFileName: string
  sizeInBytes: number
  pageCount: number
  status: string
  createdAtUtc: string
}

interface UploadDocumentResponse {
  message: string
  document: DocumentSummary
}

export interface AskDocumentResponse {
  fileName: string
  question: string
  answer: string
}

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5053',
})

export async function getDocuments(): Promise<DocumentSummary[]> {
  const response = await api.get<DocumentSummary[]>('/api/Documents')
  return response.data
}

export async function uploadDocument(
  file: File,
): Promise<UploadDocumentResponse> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await api.post<UploadDocumentResponse>(
    '/api/Documents/upload',
    formData,
  )

  return response.data
}

export async function processDocument(
  storedFileName: string,
): Promise<DocumentSummary> {
  const response = await api.post<DocumentSummary>(
    `/api/Documents/${encodeURIComponent(storedFileName)}/process`,
  )

  return response.data
}

export async function askDocument(
  storedFileName: string,
  question: string,
): Promise<AskDocumentResponse> {
  const response = await api.post<AskDocumentResponse>(
    `/api/Documents/${encodeURIComponent(storedFileName)}/ask`,
    { question },
  )

  return response.data
}

export function getApiErrorMessage(error: unknown): string {
  if (!axios.isAxiosError(error)) {
    return 'An unexpected error occurred.'
  }

  const responseData: unknown = error.response?.data

  if (typeof responseData === 'string' && responseData.trim()) {
    return responseData
  }

  if (responseData && typeof responseData === 'object') {
    const problem = responseData as {
      detail?: unknown
      title?: unknown
      errors?: unknown
    }

    if (typeof problem.detail === 'string' && problem.detail.trim()) {
      return problem.detail
    }

    if (typeof problem.title === 'string' && problem.title.trim()) {
      return problem.title
    }

    if (problem.errors && typeof problem.errors === 'object') {
      const messages = Object.values(problem.errors)
        .flat()
        .filter((message): message is string => typeof message === 'string')

      if (messages.length > 0) {
        return messages.join(' ')
      }
    }
  }

  if (!error.response) {
    return 'Could not reach the API server. Make sure the backend is running.'
  }

  return 'The request could not be completed. Please try again.'
}
