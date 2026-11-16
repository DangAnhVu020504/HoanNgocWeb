import { http, ApiEnvelope } from './http';

export interface IntegrationStatus {
  enabled: boolean;
  configured: boolean;
  provider: string;
  missing: string[];
}

export interface IntegrationsStatus {
  cod: IntegrationStatus;
  momo: IntegrationStatus;
}

export async function getIntegrationStatus(): Promise<IntegrationsStatus> {
  const res = await http.get<ApiEnvelope<IntegrationsStatus>>('/integrations/status');
  return res.data.data;
}
