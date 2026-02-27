import ENV from "../config/env.js";

const LoggerService = {
  async send(payload) {
    try {
      await fetch(`${ENV.API_BASE_URL}/telemetry/frontend-logs`, {
        method: "POST",
        mode: "cors",
        body: JSON.stringify(payload),
        keepalive: true
      });
    } catch (e) {
      console.error("Failed to send telemetry", e);
    }
  },
};

export default LoggerService;