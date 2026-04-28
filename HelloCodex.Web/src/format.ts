import type {
  QuestionAnswerType,
  QuestionInput,
  QuestionnaireDefinitionInput,
  QuestionnaireDetailsDto,
} from './contracts';

export function normalizeTagsInput(tags: string): string {
  const seenTags = new Set<string>();
  const normalizedTags: string[] = [];

  for (const tag of tags.split(',')) {
    const normalizedTag = tag.trim();
    const key = normalizedTag.toLocaleLowerCase();

    if (normalizedTag.length > 0 && !seenTags.has(key)) {
      seenTags.add(key);
      normalizedTags.push(normalizedTag);
    }
  }

  return normalizedTags.join(', ');
}

export function buildQuestionnairePayload(form: {
  title: string;
  description: string;
  tags: string;
  questions: QuestionInput[];
}): QuestionnaireDefinitionInput {
  return {
    title: form.title.trim(),
    description: form.description.trim(),
    tags: normalizeTagsInput(form.tags),
    questions: form.questions.map((question) => ({
      id: question.id,
      text: question.text.trim(),
      answerType: question.answerType,
      isRequired: question.isRequired,
    })),
  };
}

export function detailsToForm(details: QuestionnaireDetailsDto): QuestionnaireDefinitionInput {
  return {
    title: details.title,
    description: details.description,
    tags: details.tags,
    questions: details.questions.map((question) => ({
      id: question.id,
      text: question.text,
      answerType: question.answerType,
      isRequired: question.isRequired,
    })),
  };
}

export function createEmptyQuestion(): QuestionInput {
  return {
    id: null,
    text: '',
    answerType: 'Text',
    isRequired: true,
  };
}

export function answerTypeLabel(answerType: QuestionAnswerType): string {
  switch (answerType) {
    case 'Text':
      return 'Text';
    case 'YesNo':
      return 'Ja/Nein';
    case 'Likert1To5':
      return 'Likert 1-5';
  }
}

export function formatPercentage(value: number): string {
  return `${value.toLocaleString('de-DE', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  })} %`;
}

export function formatAverage(value: number | null): string {
  return value === null
    ? 'Keine Antworten'
    : value.toLocaleString('de-DE', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      });
}

export function escapeHtml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}
