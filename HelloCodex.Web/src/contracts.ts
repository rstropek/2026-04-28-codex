export type QuestionAnswerType = 'Text' | 'YesNo' | 'Likert1To5';

export interface QuestionInput {
  id: number | null;
  text: string;
  answerType: QuestionAnswerType;
  isRequired: boolean;
}

export interface QuestionnaireDefinitionInput {
  title: string;
  description: string;
  tags: string;
  questions: QuestionInput[];
}

export interface QuestionDto {
  id: number;
  text: string;
  answerType: QuestionAnswerType;
  isRequired: boolean;
  sortOrder: number;
}

export interface QuestionnaireSummaryDto {
  id: number;
  title: string;
  description: string;
  tags: string;
  hasSubmissions: boolean;
  questionCount: number;
}

export interface QuestionnaireDetailsDto extends QuestionnaireSummaryDto {
  questions: QuestionDto[];
}

export interface AnswerInput {
  questionId: number;
  textValue: string | null;
  boolValue: boolean | null;
  numberValue: number | null;
}

export interface SubmissionInput {
  answers: AnswerInput[];
}

export interface SubmissionCreatedDto {
  id: number;
}

export interface QuestionnaireResultsDto {
  id: number;
  title: string;
  questions: QuestionResultDto[];
}

export interface QuestionResultDto {
  questionId: number;
  text: string;
  answerType: QuestionAnswerType;
  textAnswers: string[];
  yesNo: YesNoResultDto | null;
  likert: LikertResultDto | null;
}

export interface YesNoResultDto {
  yesCount: number;
  noCount: number;
  yesPercentage: number;
  noPercentage: number;
}

export interface LikertResultDto {
  average: number | null;
  distribution: ValueDistributionDto[];
}

export interface ValueDistributionDto {
  value: number;
  count: number;
  percentage: number;
}

export interface ValidationProblem {
  errors?: Record<string, string[]>;
  detail?: string;
  title?: string;
}
