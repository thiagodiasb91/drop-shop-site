import LoggerService from "../services/logger.service";
import stateHelper from "./state.helper";

const getSessionId = () => {
    let sessionId = window.sessionStorage.getItem("app_session_id");
    if (!sessionId) {
        // Gera um ID único simples (UUID v4 style)
        sessionId = Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
        window.sessionStorage.setItem("app_session_id", sessionId);
    }
    return sessionId;
};

const logger = {
    local(...args) {
        if (import.meta.env.DEV) {
            // eslint-disable-next-line no-console
            console.log("[LOCAL]", ...args);
        }
    },
    debug(...args) {
        console.warn("[DEBUG]", ...args);
    },
    async error(message, errorObject = null) {
        const payload = {
            message,
            url: window.location.href,
            userEmail: stateHelper.user?.email || null,
            sessionId: getSessionId(),
            stack: errorObject?.stack,
            errorMessage: errorObject?.message,
            environment: import.meta.env.MODE,
            userAgent: navigator.userAgent,
            timestamp: new Date().toISOString()
        };

        console.error(`[SESSION:${payload.sessionId}]`, message, errorObject);

        try {
            // if (!import.meta.env.DEV)
                LoggerService.send(payload);
        } catch (e) {
            console.error(`logger.error: ${e.message}`);
        }
    }
};

export default logger;