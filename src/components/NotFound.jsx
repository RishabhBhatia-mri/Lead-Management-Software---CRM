import { Link } from "react-router-dom";
import errorImage from "../assets/7887410_3793094.svg"; // Ensure SVG is placed inside src/assets

const NotFound = () => {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen text-center bg-gray-100">
      <img src={errorImage} alt="404 Not Found" className="w-80 h-auto" />
      <h1 className="text-3xl font-bold mt-6 text-gray-800">
        Oops! Page Not Found
      </h1>
      <p className="text-gray-600 mt-2">
        The page you are looking for does not exist.
      </p>
      <Link
        to="/"
        className="mt-4 px-6 py-2 bg-blue-600 text-white rounded-lg shadow-md hover:bg-blue-700"
      >
        Go to Login
      </Link>
    </div>
  );
};

export default NotFound;
