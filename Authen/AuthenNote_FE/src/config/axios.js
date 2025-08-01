// Set config defaults when creating the instance
import axios from "axios";

// Tạo instance của axios với cấu hình mặc định
const api = axios.create({
  // baseURL: "https://localhost:7279/api/",
  // baseURL:
  //   "https://babyhaven-swp-web-emhrccb7hfh7bkf5.southeastasia-01.azurewebsites.net/api/",
  baseURL: "https://localhost:7010/api/",
  // timeout: 10000, // Thêm timeout
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },
});

// Thêm interceptor để xử lý token trong header
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    console.log("Request:", config);
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Thêm interceptor để xử lý response
api.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
      // Xử lý khi token hết hạn
      localStorage.clear();
      window.location.href = "/login";
    }
    console.error("API Error:", {
      status: error.response?.status,
      data: error.response?.data,
      config: error.config,
    });
    return Promise.reject(error);
  }
);

export default api;
