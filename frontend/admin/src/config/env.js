const IS_LOCALHOST = window.location.hostname === "localhost" || window.location.hostname === "127.0.0.1";
export const ENV = IS_LOCALHOST ? {
  SITE_URL: "http://localhost:5173",
  API_BASE_URL: "https://d2rjoik9cb60m4.cloudfront.net/test"
} : {
  SITE_URL: "https://duz838qu40buj.cloudfront.net",
  API_BASE_URL: "https://d2rjoik9cb60m4.cloudfront.net/test"
}