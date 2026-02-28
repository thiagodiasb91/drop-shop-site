import { defineConfig } from "vite";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  base: "/",    
  server: {
    port: 5173,
  },
  build: {
    outDir: "dist"
  },
  plugins: [
    tailwindcss()
  ]
});