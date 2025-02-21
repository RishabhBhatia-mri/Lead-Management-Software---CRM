import React, { useEffect, useState } from "react";

const UserManagement = () => {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);

  // Function to get the status color based on the role
  const getStatusColor = (role) => {
    switch (role) {
      case "Admin":
        return "bg-[#8650C81A] text-[#9747FF]";
      case "Manager":
        return "bg-[#FFA8001A] text-[#FFA800]";
      case "Sales Representative":
        return "bg-[#51B8BA1A] text-[#009699]";
      default:
        return "bg-gray-200 text-gray-600"; // Default color
    }
  };
  const token = sessionStorage.getItem("token");
  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const response = await fetch("http://localhost:5238/user-management", {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        });

        if (!response.ok) {
          throw new Error("Failed to fetch users");
        }

        const data = await response.json();
        setUsers(data);
      } catch (error) {
        console.error("Error fetching users:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchUsers();
  }, []);

  return (
    <div className="flex-auto min-h-screen w-[1094px]">
      <div className="text-left text-[18px] text-[#616161] font-rem font-normal mt-20">
        Home &gt; User Management
      </div>

      {/* Users table */}
      <div className="mt-10 p-6 bg-white shadow-md rounded-lg font-rem w-full ">
        <div className="text-left text-2xl mt-2 mb-4">Users</div>

        {loading ? (
          <p className="text-center text-gray-600">Loading users...</p>
        ) : (
          <table className="w-full border rounded-lg overflow-hidden">
            <thead>
              <tr className="text-left bg-[#ECECEC]">
                <th className="p-3 font-normal">Id</th>
                <th className="p-3 font-normal">Name</th>
                <th className="p-3 font-normal">Email</th>
                <th className="p-3 font-normal">Phone No</th>
                <th className="p-3 font-normal">Date of Joining</th>{" "}
                <th className="p-3 font-normal">Role</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.uid}>
                  <td className="text-left p-3 font-normal border-b">
                    {user.uid}
                  </td>
                  <td className="text-left p-3 font-normal border-b">
                    {user.name}
                  </td>
                  <td className="text-left p-3 font-normal text-[#979797] border-b">
                    {user.email}
                  </td>
                  <td className="text-left p-3 font-normal text-[#979797] border-b">
                    {user.phoneNo}
                  </td>
                  <td className="text-left p-3 font-normal text-[#979797] border-b">
                    {user.dateOfJoining}
                  </td>{" "}
                  <td className="text-left p-3 font-normal border-b">
                    <span
                      className={`px-2 py-1 rounded-md text-sm ${getStatusColor(
                        user.role
                      )}`}
                    >
                      {user.role}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default UserManagement;
