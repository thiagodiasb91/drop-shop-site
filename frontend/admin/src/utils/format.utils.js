export function currency(v) {
  return v.toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL"
  });
}

export function roleName(roleName) {
  return {
    "supplier": "Fornecedor",
    "seller": "Vendedor",
    "admin": "Admin",
  }[roleName] || "Indefinido"
}