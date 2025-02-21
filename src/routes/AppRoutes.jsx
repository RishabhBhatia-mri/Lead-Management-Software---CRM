import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";
import { useEffect, useState } from "react";
import Login from "../pages/Login";
import AdminDashboard from "../pages/AdminDashboard";
import ManagerDashboard from "../pages/ManagerDashboard";
import SalesRepDashboard from "../pages/SalesRepDashboard";
import ViewLeads from "../pages/ViewLeads";
import ViewLeadsInfo from "../pages/ViewLeadInfo";
import Profile from "../pages/Profile";
import ExportPage from "../pages/ExportPage";
import NotFound from "../components/NotFound";
import KanbanBoard from "../pages/KanbanBoard";
import AddNewLeads from "../pages/AddNewLeads";
import UserManagement from "../pages/UserManagement";
import AssignSalesRep from "../pages/AssignSalesRep";
import IndividualAssignLeadPage from "../pages/IndividualAssignLeadPage";

const PrivateRoute = ({ element }) => {
  const [userRole, setUserRole] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const token = sessionStorage.getItem("token");
        if (!token) {
          setLoading(false);
          return;
        }

        const response = await fetch("http://localhost:5238/auth/user-role", {
          method: "GET",
          headers: { Authorization: `Bearer ${token}` },
        });

        if (!response.ok) {
          console.error("Failed to fetch user role");
          setLoading(false);
          return;
        }

        const data = await response.json();
        setUserRole(data.role);
      } catch (error) {
        console.error("Error fetching role:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) return <div>Loading...</div>;
  if (!userRole) return <Navigate to="/login" />;

  if (userRole === "Admin") return <AdminDashboard />;
  if (userRole === "Manager") return <ManagerDashboard />;
  if (userRole === "Sales Representative") return <SalesRepDashboard />;

  return <Navigate to="/login" />;
};

const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" />} />
      <Route path="/login" element={<Login />} />
      <Route path="/dashboard" element={<PrivateRoute />} />
      <Route path="/Profile" element={<Profile />} />
      <Route path="/viewleads" element={<ViewLeads />} />
      <Route path="/leadinfo/:leadId" element={<ViewLeadsInfo />} />
      <Route path="/export" element={<ExportPage />} />
      <Route path="/kanban" element={<KanbanBoard />} />
      <Route path="/addlead" element={<AddNewLeads />} />
      <Route path="/userManagement" element={<UserManagement />} />
      <Route path="/assignSalesRep" element={<AssignSalesRep />} />
      <Route path="/assign/:leadId" element={<IndividualAssignLeadPage />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
};

export default AppRoutes;
