import { getStockUpdateData } from "./stock-update.js"
import { AuthService } from "../../services/auth.service.js"    

export function getData() {
    return {
        user: null,
        async init() {
            const me = await AuthService.me()

            if (!me) {
                window.location.href = "/pages/auth/login.html";
                return
            }

            this.user = me.user;
            const state = getStockUpdateData()

            const form = document.getElementById("stockForm")
            const supplierId = document.getElementById("supplierId")
            const sku = document.getElementById("sku")
            const quantity = document.getElementById("quantity")
            const submitBtn = document.getElementById("submitBtn")
            const successMessage = document.getElementById("successMessage")
            const errorMessage = document.getElementById("errorMessage")
            const loadingMessage = document.getElementById("loadingMessage")

            // Event listeners para input
            supplierId.addEventListener("change", (e) =>
                state.handleInputChange("supplierId", e.target.value)
            )
            
            sku.addEventListener("change", (e) => state.handleInputChange("sku", e.target.value))
                quantity.addEventListener("change", (e) =>
                state.handleInputChange("quantity", e.target.value)
            )


            // Submit do formulário
            form.addEventListener("submit", async (e) => {
                e.preventDefault()

                supplierId.value ? state.handleInputChange("supplierId", supplierId.value) : null
                sku.value ? state.handleInputChange("sku", sku.value) : null
                quantity.value ? state.handleInputChange("quantity", quantity.value) : null

                successMessage.style.display = "none"
                errorMessage.style.display = "none"
                loadingMessage.style.display = "block"
                submitBtn.disabled = true

                try {
                    await state.updateStock()
                    
                    if (state.success) {
                        successMessage.textContent = state.success;
                        successMessage.style.display = "block";
                        supplierId.value = "";
                        sku.value = "";
                        quantity.value = "";
                    }
                } catch (error) {
                    if (state.error) {
                        errorMessage.textContent = state.error;
                        errorMessage.style.display = "block"
                    }
                } finally {
                    loadingMessage.style.display = "none";
                    submitBtn.disabled = false
                }
            });
        },
        async logout() {
            await AuthService.logout();
            window.location.reload();
        }
    }
} 