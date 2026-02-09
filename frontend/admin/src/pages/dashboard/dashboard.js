import AuthService from "../../services/auth.service.js";
import { navigate } from "../../core/router.js";

export function getData() {
    console.log("page.dashboard.getData.called")
    return {
        async init() {
            const user = await AuthService.me();
            console.log("page.dashboard.init.called", user.role)

            // Redirecionamento inteligente baseado no cargo
            if (user.role === 'admin') {
                console.log("page.dashboard.redirect.admin")
                navigate('/admin/dashboard');
            } else if (user.role === 'seller') {
                console.log("page.dashboard.redirect.seller")
                navigate('/sellers/dashboard');
            } else if (user.role === 'supplier') {
                console.log("page.dashboard.redirect.supplier")
                navigate('/suppliers/dashboard');
            } else {
                console.log("page.dashboard.redirect.new-user")
                navigate('/new-user'); // Caso seja um usuário sem cargo ainda
            }
        }
    }
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
