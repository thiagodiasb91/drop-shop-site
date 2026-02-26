import ENV from "../config/env.js"

export const ShopeeService = {
  basePath: `${ENV.API_BASE_URL}/shopee`,  
  async getSellerAuthUrl(email) {
    console.log("ShopeeService.getSellerAuthUrl.request", email)
    const query = new URLSearchParams({ email });

    const res = await fetch(
      `${this.basePath}/auth-url?${query}`,
      {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      }
    )

    if (!res.ok) {
      console.error("ShopeeService.getSellerAuthUrl.error", res)
      throw new Error("ShopeeService.getSellerAuthUrl.error")
    }
    const response = await res.json()
    console.log("ShopeeService.getSellerAuthUrl.response", response)
    return response.authUrl
  }
}
