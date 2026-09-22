import axios from "axios";

const ADMIN_TOKEN_STORAGE_KEY = "groups_score_sheet_admin_token";

export function getAdminToken(): string {
  return localStorage.getItem(ADMIN_TOKEN_STORAGE_KEY) ?? "";
}

export function setAdminToken(token: string): void {
  localStorage.setItem(ADMIN_TOKEN_STORAGE_KEY, token.trim());
}

export function clearAdminToken(): void {
  localStorage.removeItem(ADMIN_TOKEN_STORAGE_KEY);
}

export const http = axios.create({
  baseURL: "",
  timeout: 30000,
});

http.interceptors.request.use((config) => {
  if (config.url?.startsWith("/api/admin")) {
    const token = getAdminToken();

    if (token) {
      config.headers["X-Admin-Token"] = token;
    }
  }

  return config;
});

export function getApiErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as any;

    const messages: string[] = [];

    if (data?.message) {
      messages.push(data.message);
    }

    if (Array.isArray(data?.errors)) {
      messages.push(...data.errors);
    } else if (data?.errors && typeof data.errors === "object") {
      messages.push(...Object.values(data.errors).flat().map(String));
    }

    if (messages.length > 0) {
      return messages.join("\n");
    }

    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "خطای نامشخص رخ داد.";
}
