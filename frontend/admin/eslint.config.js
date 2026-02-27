import js from "@eslint/js";
import globals from "globals";

export default [
    {
        ignores: ["dist/", "node_modules/", "build/"]
    },
  js.configs.recommended, // Configurações recomendadas do ESLint
  {
    languageOptions: {
      ecmaVersion: "latest",
      sourceType: "module",
      globals: {
        ...globals.browser,
        ...globals.node,
        // Adicione aqui globais do Alpine.js se necessário
        Alpine: "readonly"
      },
    },
    rules: {
      "no-unused-vars": ["warn", { "argsIgnorePattern": "^_" }],
      "no-console": ["warn", { "allow": ["warn", "error"] }],
      "semi": ["error", "always"],
      "quotes": ["error", "double"],
      "eqeqeq": "error"
    }
  }
];