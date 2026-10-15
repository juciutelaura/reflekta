const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080";

export interface IntentionDto {
  id: string;
  originalText: string;
  clarifiedText: string | null;
  createdAt: string;
}

export interface JourneyDto {
  id: string;
  intentionId: string;
  status: "Active" | "Completed";
  startedAt: string;
  completedAt: string | null;
}

export interface RollResultDto {
  diceResult: number;
  cardId: string;
  cardTitle: string;
  cardWisdomText: string;
  cardReflectionPrompt: string;
  cardThemes: string[];
  sequenceNumber: number;
}

export interface ReflectionDto {
  id: string;
  playedCardId: string;
  text: string;
  createdAt: string;
}

export interface JourneySummaryDto {
  id: string;
  intentionText: string;
  status: "Active" | "Completed";
  startedAt: string;
  completedAt: string | null;
  cardCount: number;
}

export interface PlayedCardDetailDto {
  id: string;
  sequenceNumber: number;
  diceResult: number;
  cardTitle: string;
  cardWisdomText: string;
  cardReflectionPrompt: string;
  cardThemes: string[];
  reflectionText: string | null;
}

export interface JourneyDetailDto {
  id: string;
  intentionText: string;
  status: "Active" | "Completed";
  startedAt: string;
  completedAt: string | null;
  playedCards: PlayedCardDetailDto[];
  summary: SessionSummaryDto | null;
}


export interface ConversationMessageDto {
  id: string;
  role: "user" | "assistant";
  content: string;
  createdAt: string;
}

export interface ReflectionWithConversationDto extends ReflectionDto {
  messages: ConversationMessageDto[];
  aiUnavailable: boolean;
}

export interface CurrentCardDto extends RollResultDto {
  reflectionText: string | null;
  messages: ConversationMessageDto[];
}

export interface SendMessageResultDto {
  userMessage: ConversationMessageDto;
  assistantMessage: ConversationMessageDto;
}

export interface ReplyResultDto {
  assistantMessage: ConversationMessageDto;
}

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, path: string) {
    super(`Request to ${path} failed with status ${status}`);
    this.name = "ApiError";
    this.status = status;
  }
}

export interface SessionSummaryDto {
  id: string;
  journeyId: string;
  summaryText: string;
  themes: string[];
  createdAt: string;
}

type GetToken = () => Promise<string | null>;

async function request<T>(getToken: GetToken, path: string, init?: RequestInit): Promise<T> {
  const token = await getToken();
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new ApiError(response.status, path);

  }

  return response.json() as Promise<T>;
}

export function createApiClient(getToken: GetToken) {
  return {
    createIntention: (text: string) =>
      request<IntentionDto>(getToken, "/api/intentions", {
        method: "POST",
        body: JSON.stringify({ text }),
      }),

    createJourney: (intentionId: string) =>
      request<JourneyDto>(getToken, "/api/journeys", {
        method: "POST",
        body: JSON.stringify({ intentionId }),
      }),

    rollDice: (journeyId: string) =>
      request<RollResultDto>(getToken, `/api/journeys/${journeyId}/roll`, { method: "POST" }),

    getCurrentCard: (journeyId: string) =>
      request<CurrentCardDto>(getToken, `/api/journeys/${journeyId}/current-card`),

    submitReflection: (journeyId: string, text: string) =>
      request<ReflectionWithConversationDto>(getToken, `/api/journeys/${journeyId}/reflection`, {
        method: "POST",
        body: JSON.stringify({ text }),
      }),

    sendMessage: (journeyId: string, content: string) =>
      request<SendMessageResultDto>(getToken, `/api/journeys/${journeyId}/messages`, {
        method: "POST",
        body: JSON.stringify({ content }),
      }),

    requestReply: (journeyId: string) =>
      request<ReplyResultDto>(getToken, `/api/journeys/${journeyId}/messages/reply`, { method: "POST" }),


    completeJourney: (journeyId: string) =>
      request<JourneyDto>(getToken, `/api/journeys/${journeyId}/complete`, { method: "POST" }),

        listJourneys: () => request<JourneySummaryDto[]>(getToken, "/api/journeys"),

    getJourneyDetail: (journeyId: string) =>
      request<JourneyDetailDto>(getToken, `/api/journeys/${journeyId}`),

     retrySummary: (journeyId: string) =>
      request<SessionSummaryDto>(getToken, `/api/journeys/${journeyId}/summary/retry`, { method: "POST" }),

  };
}

export type ApiClient = ReturnType<typeof createApiClient>;
