import { navigate } from "../../core/router.js";
import stateHelper from "../../utils/state.helper.js";
import UserRole from "../../enums/user-role.js";
import logger from "../../utils/logger.js";

export function getData() {
    logger.local("page.dashboard.getData.called");
    return {
        async init() {
            const user = stateHelper.user;
            logger.local("page.dashboard.init.called", user.role);

            // Redirecionamento inteligente baseado no cargo
            if (user.role === UserRole.ADMIN) {
                logger.local("page.dashboard.redirect.admin");
                navigate("/admin/dashboard");
            } else if (user.role === UserRole.SELLER) {
                logger.local("page.dashboard.redirect.seller");
                navigate("/sellers/dashboard");
            } else if (user.role === UserRole.SUPPLIER) {
                logger.local("page.dashboard.redirect.supplier");
                navigate("/suppliers/dashboard");
            } else if (user.role === UserRole.DISTRIBUTION_CENTER) {
                logger.local("page.dashboard.redirect.distribution-center");
                navigate("/distribution-center/dashboard");
            } else if (user.role === UserRole.NEW_USER) {
                logger.local("page.dashboard.redirect.new-user");
                navigate("/new-user"); // Caso seja um usuário sem cargo ainda
            }
        }
    };
}

export function render() {
    return `
        <div x-data="getData()" class="flex items-center justify-center h-screen bg-slate-900">
            <div class="flex flex-col items-center gap-4">
                <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-indigo-500"></div>
                <p class="text-slate-400 text-sm animate-pulse">Redirecionando...</p>
            </div>
        </div>`;
}
