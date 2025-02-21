import React, { useState } from "react";
import { useNavigate } from "react-router-dom";

const Login = () => {
  const navigate = useNavigate();

  // State for form inputs
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [loading, setLoading] = useState(false); // Loading state

  // Handle sign in
  const handleSignIn = async (e) => {
    e.preventDefault();

    // Reset error message on each attempt
    setErrorMessage("");
    setLoading(true); // Set loading to true while making the request

    try {
      // Send the login request to backend API
      const response = await fetch("http://localhost:5238/auth/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email,
          password,
        }),
      });

      const data = await response.json();
      console.log(data); // Check the response structure

      if (response.ok) {
        // On success, save the token to localStorage and redirect
        sessionStorage.setItem("token", data.token);
        setLoading(false); // Stop loading after successful login
        navigate(data.redirectTo); // This will redirect to /dashboard
      } else {
        // On error, show error message
        setLoading(false); // Stop loading on error
        setErrorMessage(data.message || "Login failed. Please try again.");
      }
    } catch (error) {
      setLoading(false); // Stop loading on error
      setErrorMessage("An error occurred. Please try again.");
      console.error("Login error:", error);
    }
  };

  return (
    <div className="flex items-center justify-center h-screen bg-gray-100">
      {/* Show Loader if loading is true */}
      {loading && (
        <div className="absolute inset-0 bg-white flex justify-center items-center z-50">
          <img
            src="src/assets/loader.svg"
            alt="Loading..."
            className="w-16 h-16"
          />
        </div>
      )}

      <div className="bg-white rounded-lg shadow-lg p-8 flex w-[900px]">
        {/* Left Section - Form */}
        <div className="w-1/2 p-4">
          <h1 className="text-4xl font-montserrat font-bold text-left mb-10">
            {" "}
            LMS{" "}
          </h1>
          <h1 className="text-2xl font-semibold py-3">Welcome to LMS!</h1>
          <form className="mt-6 py-4" onSubmit={handleSignIn}>
            <label className="block text-sm font-medium text-left text-gray-700 mb-3">
              Email
            </label>
            <input
              type="email"
              className="w-full px-4 py-2 border rounded-lg mt-1"
              placeholder="Enter your email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />

            <label className="block text-sm font-medium text-left text-gray-700 mt-6">
              Password
            </label>
            <div className="relative my-2">
              <input
                type="password"
                className="w-full px-4 py-2 border rounded-lg mt-1"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>

            {errorMessage && (
              <div className="text-red-500 text-sm mt-2">{errorMessage}</div>
            )}

            <div className="flex justify-between text-sm text-gray-500 mt-8">
              <span className="cursor-pointer">Forgot password?</span>
            </div>

            <button
              className="w-full bg-teal-600 text-white py-2 mt-4 rounded-lg"
              type="submit"
            >
              Login
            </button>
          </form>
        </div>

        {/* Right Section - Illustration */}
        <div className="w-1/2 flex items-center justify-center">
          <img
            src="src/assets/SignIn_Placeholder.svg"
            alt="Illustration"
            className="max-w-full h-auto"
          />
        </div>
      </div>
    </div>
  );
};

export default Login;
