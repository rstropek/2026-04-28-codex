import './style.css';
import {
  ApiError,
  createQuestionnaire,
  getQuestionnaire,
  getResults,
  listQuestionnaires,
  submitAnswers,
  updateQuestionnaire,
} from './api';
import type {
  AnswerInput,
  QuestionAnswerType,
  QuestionnaireDefinitionInput,
  QuestionnaireDetailsDto,
  QuestionnaireResultsDto,
  QuestionnaireSummaryDto,
} from './contracts';
import {
  answerTypeLabel,
  buildQuestionnairePayload,
  createEmptyQuestion,
  detailsToForm,
  escapeHtml,
  formatAverage,
  formatPercentage,
} from './format';

type ViewName = 'manage' | 'answer' | 'results';
type MessageTone = 'info' | 'success' | 'error';

interface AppMessage {
  tone: MessageTone;
  text: string;
}

interface AppState {
  view: ViewName;
  questionnaires: QuestionnaireSummaryDto[];
  manageSelection: number | 'new';
  manageForm: QuestionnaireDefinitionInput;
  manageDetails: QuestionnaireDetailsDto | null;
  answerSelection: number | null;
  resultsSelection: number | null;
  results: QuestionnaireResultsDto | null;
  message: AppMessage | null;
  isLoading: boolean;
}

const app = document.querySelector<HTMLDivElement>('#app');

if (app === null) {
  throw new Error('App root element was not found.');
}

const appRoot = app;

const state: AppState = {
  view: 'manage',
  questionnaires: [],
  manageSelection: 'new',
  manageForm: createNewForm(),
  manageDetails: null,
  answerSelection: null,
  resultsSelection: null,
  results: null,
  message: null,
  isLoading: true,
};

void initialize();

async function initialize(): Promise<void> {
  await refreshQuestionnaires();
  state.isLoading = false;
  render();
}

async function refreshQuestionnaires(): Promise<void> {
  try {
    state.questionnaires = await listQuestionnaires();
    state.answerSelection ??= state.questionnaires[0]?.id ?? null;
    state.resultsSelection ??= state.questionnaires[0]?.id ?? null;
  } catch (error) {
    setMessage('error', getErrorMessage(error));
  }
}

function render(): void {
  appRoot.innerHTML = `
    <div class="shell">
      <header class="top-bar">
        <a class="brand" href="/" aria-label="HelloCodex home">
          <img src="/favicon.svg" alt="" width="32" height="32" />
          <span>HelloCodex</span>
        </a>
        <nav aria-label="Hauptnavigation">
          ${renderNavButton('manage', 'Verwalten')}
          ${renderNavButton('answer', 'Beantworten')}
          ${renderNavButton('results', 'Auswertung')}
        </nav>
      </header>
      <main class="content">
        ${state.message === null ? '' : renderMessage(state.message)}
        ${state.isLoading ? '<p class="empty-state">Daten werden geladen...</p>' : renderCurrentView()}
      </main>
    </div>
  `;

  bindEvents();
}

function renderNavButton(view: ViewName, label: string): string {
  const current = state.view === view ? ' aria-current="page"' : '';
  return `<button type="button" class="nav-button" data-view="${view}"${current}>${label}</button>`;
}

function renderMessage(message: AppMessage): string {
  return `<div class="message message-${message.tone}" role="status">${escapeHtml(message.text)}</div>`;
}

function renderCurrentView(): string {
  switch (state.view) {
    case 'manage':
      return renderManageView();
    case 'answer':
      return renderAnswerView();
    case 'results':
      return renderResultsView();
  }
}

function renderManageView(): string {
  return `
    <section class="workspace">
      <aside class="list-panel" aria-label="Fragebögen">
        <button type="button" class="primary-action" id="new-questionnaire">Neuer Fragebogen</button>
        <div class="questionnaire-list">
          ${state.questionnaires.length === 0 ? '<p class="empty-state">Noch keine Fragebögen.</p>' : ''}
          ${state.questionnaires.map(renderQuestionnaireListItem).join('')}
        </div>
      </aside>
      <section class="editor-panel" aria-labelledby="manage-title">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">${state.manageSelection === 'new' ? 'Erstellen' : 'Bearbeiten'}</p>
            <h1 id="manage-title">Fragebogen</h1>
          </div>
          ${
            state.manageDetails?.hasSubmissions
              ? '<span class="lock-badge">Antworten vorhanden</span>'
              : ''
          }
        </div>
        ${renderQuestionnaireForm()}
      </section>
    </section>
  `;
}

function renderQuestionnaireListItem(questionnaire: QuestionnaireSummaryDto): string {
  const selected = state.manageSelection === questionnaire.id ? ' is-selected' : '';
  const tags = questionnaire.tags.length > 0 ? questionnaire.tags : 'Keine Tags';

  return `
    <button type="button" class="list-item${selected}" data-manage-id="${questionnaire.id}">
      <span>${escapeHtml(questionnaire.title)}</span>
      <small>${questionnaire.questionCount} Fragen · ${escapeHtml(tags)}</small>
    </button>
  `;
}

function renderQuestionnaireForm(): string {
  const disabled = state.manageDetails?.hasSubmissions === true;
  const disabledAttribute = disabled ? ' disabled' : '';

  return `
    <form id="questionnaire-form" class="form-stack">
      <label>
        <span>Titel</span>
        <input name="title" maxlength="50" value="${escapeHtml(state.manageForm.title)}"${disabledAttribute} />
      </label>
      <label>
        <span>Beschreibung</span>
        <textarea name="description" maxlength="200"${disabledAttribute}>${escapeHtml(
          state.manageForm.description,
        )}</textarea>
      </label>
      <label>
        <span>Tags</span>
        <input name="tags" value="${escapeHtml(state.manageForm.tags)}"${disabledAttribute} />
      </label>
      <div class="questions-heading">
        <h2>Fragen</h2>
        <button type="button" class="secondary-action" id="add-question"${disabledAttribute}>Frage hinzufügen</button>
      </div>
      <div class="question-editor-list">
        ${state.manageForm.questions.map((question, index) => renderQuestionEditor(question, index, disabled)).join('')}
      </div>
      <div class="form-actions">
        <button type="submit" class="primary-action"${disabledAttribute}>Speichern</button>
      </div>
    </form>
  `;
}

function renderQuestionEditor(
  question: QuestionnaireDefinitionInput['questions'][number],
  index: number,
  disabled: boolean,
): string {
  const disabledAttribute = disabled ? ' disabled' : '';

  return `
    <section class="question-editor" data-question-index="${index}">
      <h3>Frage ${index + 1}</h3>
      <label>
        <span>Text</span>
        <textarea name="question-text" maxlength="500"${disabledAttribute}>${escapeHtml(question.text)}</textarea>
      </label>
      <div class="inline-fields">
        <label>
          <span>Antworttyp</span>
          <select name="question-type"${disabledAttribute}>
            ${renderAnswerTypeOption('Text', question.answerType)}
            ${renderAnswerTypeOption('YesNo', question.answerType)}
            ${renderAnswerTypeOption('Likert1To5', question.answerType)}
          </select>
        </label>
        <label class="checkbox-label">
          <input name="question-required" type="checkbox"${question.isRequired ? ' checked' : ''}${disabledAttribute} />
          <span>Pflichtfrage</span>
        </label>
      </div>
    </section>
  `;
}

function renderAnswerTypeOption(
  value: QuestionAnswerType,
  selectedValue: QuestionAnswerType,
): string {
  return `<option value="${value}"${value === selectedValue ? ' selected' : ''}>${answerTypeLabel(value)}</option>`;
}

function renderAnswerView(): string {
  const questionnaire = findQuestionnaire(state.answerSelection);

  return `
    <section class="single-panel" aria-labelledby="answer-title">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">Beantwortung</p>
          <h1 id="answer-title">Antworten erfassen</h1>
        </div>
        ${renderQuestionnaireSelect('answer-select', state.answerSelection)}
      </div>
      ${
        questionnaire === null
          ? '<p class="empty-state">Es gibt noch keinen Fragebogen.</p>'
          : renderAnswerForm(questionnaire)
      }
    </section>
  `;
}

function renderAnswerForm(questionnaire: QuestionnaireSummaryDto): string {
  return `
    <form id="answer-form" class="form-stack">
      <div class="answer-list" id="answer-list">
        <p class="empty-state">Fragen werden geladen...</p>
      </div>
      <div class="form-actions">
        <button type="submit" class="primary-action">Antworten speichern</button>
      </div>
      <input type="hidden" name="questionnaire-id" value="${questionnaire.id}" />
    </form>
  `;
}

function renderResultsView(): string {
  return `
    <section class="single-panel" aria-labelledby="results-title">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">Auswertung</p>
          <h1 id="results-title">Antworten anzeigen</h1>
        </div>
        ${renderQuestionnaireSelect('results-select', state.resultsSelection)}
      </div>
      <div id="results-content">
        ${state.results === null ? '<p class="empty-state">Wähle einen Fragebogen aus.</p>' : renderResults(state.results)}
      </div>
    </section>
  `;
}

function renderQuestionnaireSelect(id: string, selectedId: number | null): string {
  return `
    <label class="compact-select">
      <span>Fragebogen</span>
      <select id="${id}">
        ${state.questionnaires
          .map(
            (questionnaire) =>
              `<option value="${questionnaire.id}"${questionnaire.id === selectedId ? ' selected' : ''}>${escapeHtml(
                questionnaire.title,
              )}</option>`,
          )
          .join('')}
      </select>
    </label>
  `;
}

function renderResults(results: QuestionnaireResultsDto): string {
  return `
    <div class="results-list">
      ${results.questions.map(renderQuestionResult).join('')}
    </div>
  `;
}

function renderQuestionResult(question: QuestionnaireResultsDto['questions'][number]): string {
  if (question.answerType === 'Text') {
    return `
      <section class="result-block">
        <h2>${escapeHtml(question.text)}</h2>
        ${
          question.textAnswers.length === 0
            ? '<p class="empty-state">Keine Antworten.</p>'
            : `<ul class="text-results">${question.textAnswers.map((answer) => `<li>${escapeHtml(answer)}</li>`).join('')}</ul>`
        }
      </section>
    `;
  }

  if (question.answerType === 'YesNo' && question.yesNo !== null) {
    return `
      <section class="result-block">
        <h2>${escapeHtml(question.text)}</h2>
        <div class="metric-grid">
          <div><strong>${question.yesNo.yesCount}</strong><span>Ja · ${formatPercentage(question.yesNo.yesPercentage)}</span></div>
          <div><strong>${question.yesNo.noCount}</strong><span>Nein · ${formatPercentage(question.yesNo.noPercentage)}</span></div>
        </div>
      </section>
    `;
  }

  return `
    <section class="result-block">
      <h2>${escapeHtml(question.text)}</h2>
      <p class="average">Durchschnitt: ${formatAverage(question.likert?.average ?? null)}</p>
      <div class="distribution">
        ${(question.likert?.distribution ?? [])
          .map(
            (bucket) => `
              <div class="distribution-row">
                <span>${bucket.value}</span>
                <div><span style="width: ${bucket.percentage}%"></span></div>
                <strong>${formatPercentage(bucket.percentage)}</strong>
              </div>
            `,
          )
          .join('')}
      </div>
    </section>
  `;
}

function bindEvents(): void {
  document.querySelectorAll<HTMLButtonElement>('[data-view]').forEach((button) => {
    button.addEventListener('click', () => {
      state.view = button.dataset.view as ViewName;
      state.message = null;
      render();
      if (state.view === 'answer') {
        void loadAnswerQuestions();
      }
      if (state.view === 'results') {
        void loadResults();
      }
    });
  });

  document.querySelector<HTMLButtonElement>('#new-questionnaire')?.addEventListener('click', () => {
    state.manageSelection = 'new';
    state.manageDetails = null;
    state.manageForm = createNewForm();
    state.message = null;
    render();
  });

  document.querySelectorAll<HTMLButtonElement>('[data-manage-id]').forEach((button) => {
    button.addEventListener('click', () => {
      const id = Number(button.dataset.manageId);
      void selectQuestionnaireForManagement(id);
    });
  });

  document.querySelector<HTMLButtonElement>('#add-question')?.addEventListener('click', () => {
    state.manageForm = readQuestionnaireForm();
    state.manageForm.questions.push(createEmptyQuestion());
    render();
  });

  document
    .querySelector<HTMLFormElement>('#questionnaire-form')
    ?.addEventListener('submit', (event) => {
      event.preventDefault();
      void saveQuestionnaire();
    });

  document
    .querySelector<HTMLSelectElement>('#answer-select')
    ?.addEventListener('change', (event) => {
      state.answerSelection = Number((event.currentTarget as HTMLSelectElement).value);
      void loadAnswerQuestions();
    });

  document.querySelector<HTMLFormElement>('#answer-form')?.addEventListener('submit', (event) => {
    event.preventDefault();
    void saveAnswers();
  });

  document
    .querySelector<HTMLSelectElement>('#results-select')
    ?.addEventListener('change', (event) => {
      state.resultsSelection = Number((event.currentTarget as HTMLSelectElement).value);
      void loadResults();
    });
}

async function selectQuestionnaireForManagement(id: number): Promise<void> {
  try {
    state.manageSelection = id;
    state.manageDetails = await getQuestionnaire(id);
    state.manageForm = detailsToForm(state.manageDetails);
    state.message = null;
    render();
  } catch (error) {
    setMessage('error', getErrorMessage(error));
  }
}

async function saveQuestionnaire(): Promise<void> {
  try {
    const payload = buildQuestionnairePayload(readQuestionnaireForm());
    const saved =
      state.manageSelection === 'new'
        ? await createQuestionnaire(payload)
        : await updateQuestionnaire(state.manageSelection, payload);

    state.manageSelection = saved.id;
    state.manageDetails = saved;
    state.manageForm = detailsToForm(saved);
    await refreshQuestionnaires();
    setMessage('success', 'Der Fragebogen wurde gespeichert.');
  } catch (error) {
    setMessage('error', getErrorMessage(error));
  }
}

async function loadAnswerQuestions(): Promise<void> {
  const answerList = document.querySelector<HTMLDivElement>('#answer-list');
  if (answerList === null || state.answerSelection === null) {
    return;
  }

  try {
    const questionnaire = await getQuestionnaire(state.answerSelection);
    answerList.innerHTML = questionnaire.questions.map(renderAnswerQuestion).join('');
  } catch (error) {
    setMessage('error', getErrorMessage(error));
  }
}

function renderAnswerQuestion(question: QuestionnaireDetailsDto['questions'][number]): string {
  return `
    <section class="answer-question" data-answer-question-id="${question.id}" data-answer-type="${question.answerType}">
      <h3>${escapeHtml(question.text)}${question.isRequired ? ' <span>Pflicht</span>' : ''}</h3>
      ${renderAnswerControl(question)}
    </section>
  `;
}

function renderAnswerControl(question: QuestionnaireDetailsDto['questions'][number]): string {
  if (question.answerType === 'Text') {
    return '<textarea name="text-answer" maxlength="4000"></textarea>';
  }

  if (question.answerType === 'YesNo') {
    return `
      <select name="yes-no-answer">
        <option value="">Keine Auswahl</option>
        <option value="true">Ja</option>
        <option value="false">Nein</option>
      </select>
    `;
  }

  return `
    <div class="likert-group">
      ${[1, 2, 3, 4, 5]
        .map(
          (value) => `
            <label>
              <input type="radio" name="likert-${question.id}" value="${value}" />
              <span>${value}</span>
            </label>
          `,
        )
        .join('')}
    </div>
  `;
}

async function saveAnswers(): Promise<void> {
  if (state.answerSelection === null) {
    return;
  }

  try {
    await submitAnswers(state.answerSelection, { answers: readAnswers() });
    await refreshQuestionnaires();
    state.message = { tone: 'success', text: 'Die Antworten wurden gespeichert.' };
    render();
    await loadAnswerQuestions();
  } catch (error) {
    setMessage('error', getErrorMessage(error));
  }
}

async function loadResults(): Promise<void> {
  if (state.resultsSelection === null) {
    state.results = null;
    render();
    return;
  }

  try {
    state.results = await getResults(state.resultsSelection);
    state.message = null;
    render();
  } catch (error) {
    setMessage('error', getErrorMessage(error));
  }
}

function readQuestionnaireForm(): QuestionnaireDefinitionInput {
  const form = document.querySelector<HTMLFormElement>('#questionnaire-form');
  if (form === null) {
    return state.manageForm;
  }

  const formData = new FormData(form);
  const questions = Array.from(form.querySelectorAll<HTMLElement>('[data-question-index]')).map(
    (questionElement, index) => {
      const currentQuestion = state.manageForm.questions[index] ?? createEmptyQuestion();
      const text =
        questionElement.querySelector<HTMLTextAreaElement>('[name="question-text"]')?.value ?? '';
      const answerType =
        (questionElement.querySelector<HTMLSelectElement>('[name="question-type"]')
          ?.value as QuestionAnswerType) ?? 'Text';
      const isRequired =
        questionElement.querySelector<HTMLInputElement>('[name="question-required"]')?.checked ??
        false;

      return {
        id: currentQuestion.id,
        text,
        answerType,
        isRequired,
      };
    },
  );

  return {
    title: String(formData.get('title') ?? ''),
    description: String(formData.get('description') ?? ''),
    tags: String(formData.get('tags') ?? ''),
    questions,
  };
}

function readAnswers(): AnswerInput[] {
  return Array.from(document.querySelectorAll<HTMLElement>('[data-answer-question-id]')).map(
    (questionElement) => {
      const questionId = Number(questionElement.dataset.answerQuestionId);
      const answerType = questionElement.dataset.answerType as QuestionAnswerType;

      if (answerType === 'Text') {
        const textValue =
          questionElement
            .querySelector<HTMLTextAreaElement>('[name="text-answer"]')
            ?.value.trim() ?? '';
        return {
          questionId,
          textValue: textValue.length === 0 ? null : textValue,
          boolValue: null,
          numberValue: null,
        };
      }

      if (answerType === 'YesNo') {
        const value =
          questionElement.querySelector<HTMLSelectElement>('[name="yes-no-answer"]')?.value ?? '';
        return {
          questionId,
          textValue: null,
          boolValue: value === '' ? null : value === 'true',
          numberValue: null,
        };
      }

      const checkedValue = questionElement.querySelector<HTMLInputElement>(
        'input[type="radio"]:checked',
      )?.value;
      return {
        questionId,
        textValue: null,
        boolValue: null,
        numberValue: checkedValue === undefined ? null : Number(checkedValue),
      };
    },
  );
}

function findQuestionnaire(id: number | null): QuestionnaireSummaryDto | null {
  return state.questionnaires.find((questionnaire) => questionnaire.id === id) ?? null;
}

function createNewForm(): QuestionnaireDefinitionInput {
  return {
    title: '',
    description: '',
    tags: '',
    questions: [createEmptyQuestion()],
  };
}

function setMessage(tone: MessageTone, text: string): void {
  state.message = { tone, text };
  render();
}

function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Der Vorgang konnte nicht abgeschlossen werden.';
}
