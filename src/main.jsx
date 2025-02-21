import ReactDOM from "react-dom/client";
import React from "react";
import { BrowserRouter } from 'react-router-dom';
import './index.css'
import App from './App.jsx'

ReactDOM.createRoot(document.getElementById('root')).render(
  <BrowserRouter>
    <App />
  </BrowserRouter>
)
