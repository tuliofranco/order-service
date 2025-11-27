export default {
  mongodb: {
    server: "mongo",
    port: 27017,
    ssl: false,
    auth: [
      {
        database: "admin",
        username: "root",
        password: "secret123"
      }
    ]
  },
  site: {
    baseUrl: "/",
    cookieKeyName: "mongo-express"
  },
  useBasicAuth: false
};
