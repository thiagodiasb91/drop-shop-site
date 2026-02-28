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
    "distribution_center": "Centro de Distribuição",
  }[roleName] || "Indefinido";
}

export const maskCNPJ = (v) => {
  v = v.replace(/[^a-zA-Z0-9]/g, "").toUpperCase().slice(0, 14);
  
  if (v.length <= 2) return v;
  if (v.length <= 5) return v.replace(/^(\w{2})(\w*)/, "$1.$2");
  if (v.length <= 8) return v.replace(/^(\w{2})(\w{3})(\w*)/, "$1.$2.$3");
  if (v.length <= 12) return v.replace(/^(\w{2})(\w{3})(\w{3})(\w*)/, "$1.$2.$3/$4");
  return v.replace(/^(\w{2})(\w{3})(\w{3})(\w{4})(\w{2})/, "$1.$2.$3/$4-$5");
};

export const maskPhone = (v) => {
  v = v.replace(/\D/g, "").slice(0, 11);
  if (v.length > 10) {
    return v.replace(/^(\d{2})(\d{5})(\d{4}).*/, "($1) $2-$3");
  }
  return v.replace(/^(\d{2})(\d{4})(\d{4}).*/, "($1) $2-$3");
};