import { describe, expect, it } from 'vitest';
import {
  answerTypeLabel,
  buildQuestionnairePayload,
  formatAverage,
  formatPercentage,
  normalizeTagsInput,
} from './format';

describe('questionnaire formatting helpers', () => {
  it('normalizes comma-separated tags', () => {
    expect(normalizeTagsInput(' kunden, Q1, kunden ,, intern ')).toBe('kunden, Q1, intern');
  });

  it('builds a trimmed questionnaire payload', () => {
    const payload = buildQuestionnairePayload({
      title: '  Feedback  ',
      description: '  Quartal  ',
      tags: ' A, B, a ',
      questions: [
        {
          id: null,
          text: '  Empfehlung?  ',
          answerType: 'YesNo',
          isRequired: true,
        },
      ],
    });

    expect(payload).toEqual({
      title: 'Feedback',
      description: 'Quartal',
      tags: 'A, B',
      questions: [
        {
          id: null,
          text: 'Empfehlung?',
          answerType: 'YesNo',
          isRequired: true,
        },
      ],
    });
  });

  it('formats result values for German UI output', () => {
    expect(answerTypeLabel('Likert1To5')).toBe('Likert 1-5');
    expect(formatPercentage(33.333)).toBe('33,3 %');
    expect(formatAverage(4)).toBe('4,00');
    expect(formatAverage(null)).toBe('Keine Antworten');
  });
});
