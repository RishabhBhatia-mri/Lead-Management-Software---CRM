/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        montserrat: ['Montserrat', 'sans-serif'],
        montserrat: ['Montserrat Alternates', 'sans-serif'],
        rem: ["REM", "sans-serif"],
      }
    },
  },
  plugins: [],
};
