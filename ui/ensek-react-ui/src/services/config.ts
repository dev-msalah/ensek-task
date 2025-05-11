import configData from '../config.json';

interface AppConfig {
  apiBaseUrl: string;
  maxFileSizeInMB: number;
}

const config: AppConfig = {
  apiBaseUrl: configData.apiBaseUrl,
  maxFileSizeInMB: configData.maxFileSizeInMB || 5
};

export default config;