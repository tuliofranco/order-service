import axios from "axios";

export const IA_API_URL = process.env.NEXT_PUBLIC_IA_API_URL;

export const apiIa = axios.create({
  baseURL: IA_API_URL,
});

apiIa.interceptors.response.use(
  (res) => res,
  (err) => Promise.reject(err)
);
