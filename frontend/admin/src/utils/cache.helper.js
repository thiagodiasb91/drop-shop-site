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
    // console.log("cache.set.value", name, value)
    sessionStorage.setItem(name, value)
  },
  remove(name){
    try{
      // console.log("cache.remove.request", name)
      sessionStorage.removeItem(name)
    } catch(err) {
      // console.log("cache.remove.err", err)
    }
  }
}

export default CacheHelper