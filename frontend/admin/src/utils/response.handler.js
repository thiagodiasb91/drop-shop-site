export async function responseHandler(res) {
  let response = null
  try {
    response = await res.json()
    response = response.items ? response.items : response
  } catch {}

  console.log("responseHandler",{
    ok: res.ok,
    status: res.status,
    response
  })

  return {
    ok: res.ok,
    status: res.status,
    response
  }
}