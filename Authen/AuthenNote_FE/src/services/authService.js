import api from "../config/axios";

export const loginWithGoogle = async (idToken) => {
  const response = await api.post("GoogleAuthentication/loginbygoogle", {
    idToken: idToken,
  });
  return response.data;
};
