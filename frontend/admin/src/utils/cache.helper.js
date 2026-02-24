const CacheHelper = {
  get(name) {
    const cached = sessionStorage.getItem(name)    
    try{
      const response = JSON.parse(cached)
      console.log("cache.get.response.json", name, response)
      return response
    }
    catch{
      console.log("cache.get.response", name, cached)
      return cached
    }
  },
  set (name, value) {
    value = JSON.stringify(value)
    sessionStorage.setItem(name, value)
  },
  remove(name){
    try{
      sessionStorage.removeItem(name)
    } catch(err) {
    }
  }
}

export default CacheHelper