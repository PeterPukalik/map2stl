import React, { useEffect, useState } from "react";
import { fetchAllUsers, resetUserPassword } from "../../services/api";

const Admin = ({ token }) => {
  const [users, setUsers] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const data = await fetchAllUsers(token);
        setUsers(data);
      } catch (err) {
        setError(err.message);
      }
    };

    fetchUsers();
  }, [token]);

  const handleResetPassword = async (userId) => {
    try {
      const response = await resetUserPassword(userId, token);
      alert(response.message);
    } catch (err) {
      alert(`Error resetting password: ${err.message}`);
    }
  };

  if (error) return <p>Error: {error}</p>;

  return (
    <div>
      <h2>Admin Dashboard</h2>
      <table>
        <thead>
          <tr>
            <th>Username</th>
            <th>Email</th>
            <th>Role</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={user.id}>
              <td>{user.username}</td>
              <td>{user.email}</td>
              <td>{user.role}</td>
              <td>
                <button onClick={() => handleResetPassword(user.id)}>
                  Reset Password
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default Admin;
