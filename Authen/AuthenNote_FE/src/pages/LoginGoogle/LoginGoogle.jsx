import { GoogleLogin } from '@react-oauth/google';
import { loginWithGoogle } from '../../services/authService';

const LoginGoogle = () => {
  const handleSuccess = async (credentialResponse) => {
    try {
      const idToken = credentialResponse.credential;
      if (!idToken) {
        alert('Không nhận được ID token từ Google');
        return;
      }

      const data = await loginWithGoogle(idToken);
      localStorage.setItem('accessToken', data.token);
      alert('Đăng nhập thành công!');
    } catch (error) {
      console.error('Login thất bại:', error);
      alert('Đăng nhập thất bại!');
    }
  };

  return (
    <div style={{ marginTop: '100px', textAlign: 'center' }}>
      <GoogleLogin
        onSuccess={handleSuccess}
        onError={() => {
          alert('Google Login thất bại!');
        }}
        useOneTap // ✅ Tùy chọn: nếu bạn muốn One Tap hiển thị popup tự động
      />
    </div>
  );
};

export default LoginGoogle;






// import { useGoogleLogin } from '@react-oauth/google';
// import { loginWithGoogle } from '../../services/authService';

// const LoginGoogle = () => {
//   const login = useGoogleLogin({
//     flow: 'implicit', // dùng implicit flow để lấy id_token trực tiếp
//     onSuccess: async (response) => {
//       try {
//         const idToken = response.credential;
//         const data = await loginWithGoogle(idToken);
//         localStorage.setItem('accessToken', data.token);
//         alert('Đăng nhập thành công!');
//       } catch (error) {
//         console.error('Login thất bại:', error);
//         alert('Đăng nhập thất bại!');
//       }
//     },
//     onError: () => {
//       alert('Google Login thất bại!');
//     },
//   });

//   return (
//     <div style={{ marginTop: '100px', textAlign: 'center' }}>
//       <button onClick={() => login()}>
//         Đăng nhập với Google
//       </button>
//     </div>
//   );
// };

// export default LoginGoogle;
