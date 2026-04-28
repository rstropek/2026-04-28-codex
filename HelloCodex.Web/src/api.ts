import type {
  QuestionnaireDefinitionInput,
  QuestionnaireDetailsDto,
  QuestionnaireResultsDto,
  QuestionnaireSummaryDto,
  SubmissionCreatedDto,
  SubmissionInput,
  ValidationProblem,
} from './contracts';

export class ApiError extends Error {
  public readonly status: number;

  public readonly validationErrors: string[];

  constructor(message: string, status: number, validationErrors: string[] = []) {
    super(message);
    this.status = status;
    this.validationErrors = validationErrors;
  }
}

export async function listQuestionnaires(): Promise<QuestionnaireSummaryDto[]> {
  return getJson<QuestionnaireSummaryDto[]>('/api/questionnaires');
}

export async function getQuestionnaire(id: number): Promise<QuestionnaireDetailsDto> {
  return getJson<QuestionnaireDetailsDto>(`/api/questionnaires/${id}`);
}

export async function createQuestionnaire(
  input: QuestionnaireDefinitionInput,
): Promise<QuestionnaireDetailsDto> {
  return sendJson<QuestionnaireDetailsDto>('/api/questionnaires', 'POST', input);
}

export async function updateQuestionnaire(
  id: number,
  input: QuestionnaireDefinitionInput,
): Promise<QuestionnaireDetailsDto> {
  return sendJson<QuestionnaireDetailsDto>(`/api/questionnaires/${id}`, 'PUT', input);
}

export async function submitAnswers(
  questionnaireId: number,
  input: SubmissionInput,
): Promise<SubmissionCreatedDto> {
  return sendJson<SubmissionCreatedDto>(
    `/api/questionnaires/${questionnaireId}/submissions`,
    'POST',
    input,
  );
}

export async function getResults(questionnaireId: number): Promise<QuestionnaireResultsDto> {
  return getJson<QuestionnaireResultsDto>(`/api/questionnaires/${questionnaireId}/results`);
}

async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url);
  return readResponse<T>(response);
}

async function sendJson<T>(url: string, method: string, body: unknown): Promise<T> {
  const response = await fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  return readResponse<T>(response);
}

async function readResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    return (await response.json()) as T;
  }

  const problem = await readProblem(response);
  const validationErrors = Object.values(problem.errors ?? {}).flat();
  const message =
    validationErrors.length > 0
      ? validationErrors.join(' ')
      : (problem.detail ?? problem.title ?? `HTTP ${response.status}`);

  throw new ApiError(message, response.status, validationErrors);
}

async function readProblem(response: Response): Promise<ValidationProblem> {
  try {
    return (await response.json()) as ValidationProblem;
  } catch {
    return {};
  }
}
