import axios from "axios";

export const API_URL =
  process.env.NEXT_PUBLIC_API_URL;

export const api = axios.create({
  baseURL: API_URL,
});

api.interceptors.request.use(
  (config) => {

    if (config.headers && "Authorization" in config.headers) {
      delete config.headers.Authorization;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (res) => res,
  (err) => Promise.reject(err)
);
