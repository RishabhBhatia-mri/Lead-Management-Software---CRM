import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";

const statusOptions = ["New", "Contacted", "Follow-up", "Converted", "Lost"];
const FollowUpStatusOptions = ["Pending", "Completed", "Missed"];

const ViewLeadInfo = () => {
  const [leadData, setLeadData] = useState(null);
  const [isEditing, setIsEditing] = useState(false);
  const [originalData, setOriginalData] = useState(null);
  const [newPhone, setNewPhone] = useState("");
  const [newStatus, setNewStatus] = useState("");
  const [newNotes, setNewNotes] = useState("");
  const [error, setError] = useState(null);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const { leadId } = useParams();
  const navigate = useNavigate();
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [followUpDate, setFollowUpDate] = useState("");
  const [followUpStatus, setFollowUpStatus] = useState("Pending"); // Default to "Pending"
  const [followUpNotes, setFollowUpNotes] = useState("");

  const token = sessionStorage.getItem("token");

  // Fetch lead data
  useEffect(() => {
    const fetchLeadData = async () => {
      if (!token) return;

      try {
        const response = await fetch(
          `http://localhost:5238/leadInfo/${leadId}`,
          {
            headers: { Authorization: `Bearer ${token}` },
          }
        );

        if (!response.ok) throw new Error("Failed to fetch lead data");

        const data = await response.json();
        setLeadData(data);
        setOriginalData(data);
        setNewPhone(data.phone);
        setNewStatus(data.status);
      } catch (error) {
        setError(error.message);
      }
    };

    fetchLeadData();
  }, [leadId, token]);

  // Update Phone and Status
  const handleUpdateLead = async () => {
    setShowConfirmModal(false);

    // Phone Validation: Ensure the phone number is exactly 10 digits
    if (!/^\d{10}$/.test(newPhone)) {
      alert("Phone number must be exactly 10 digits.");
      return;
    }

    try {
      // Update Phone
      const updatePhoneRes = await fetch(
        `http://localhost:5238/leads/update-phone/${leadId}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ newPhone }), // Ensure proper format
        }
      );

      // Update Status (already working, no changes)
      const updateStatusRes = await fetch(
        `http://localhost:5238/leads/update-status/${leadId}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ newStatus }),
        }
      );

      // Add Follow-up Note
      const updateNoteRes = await fetch(
        `http://localhost:5238/leads/add-note/${leadId}`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ notes: newNotes, responded: false }),
        }
      );

      // Check API responses
      if (!updatePhoneRes.ok) throw new Error("Failed to update phone");
      if (!updateStatusRes.ok) throw new Error("Failed to update status");
      if (!updateNoteRes.ok) throw new Error("Failed to add note");

      // Update UI after successful API calls
      setLeadData((prev) => ({
        ...prev,
        phone: newPhone,
        status: newStatus,
        followUpNotes: [
          ...(prev.followUpNotes || []),
          { notes: newNotes, followUpDate: new Date().toISOString() },
        ],
      }));

      setNewNotes(""); // Clear input after adding note
      setIsEditing(false);
      alert("Lead details updated successfully!");
    } catch (error) {
      setError(error.message);
    }
  };

  // Cancel Edit
  const handleCancelEdit = () => {
    setNewPhone(leadData.phone);
    setNewStatus(leadData.status);
    setIsEditing(false);
  };

  // Open confirmation modal
  const handleSaveClick = () => {
    setShowConfirmModal(true);
  };

  // Delete Lead
  const handleDeleteLead = async () => {
    if (!window.confirm("Are you sure you want to delete this lead?")) return;

    try {
      const response = await fetch(`http://localhost:5238/leadInfo/${leadId}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!response.ok) throw new Error("Failed to delete lead");

      setShowDeleteModal(false);
      navigate("/viewleads");
    } catch (error) {
      alert(error.message);
    }
  };

  // Follow ups
  const handleAddFollowUp = async () => {
    if (!followUpDate || !followUpNotes) {
      alert("Please fill in the follow-up date and notes.");
      return;
    }

    try {
      const response = await fetch(
        `http://localhost:5238/leads/add-followup/${leadId}`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({
            FollowUpDate: followUpDate,
            Status: followUpStatus.trim(), // Ensure no extra spaces
            Notes: followUpNotes,
          }),
        }
      );

      if (!response.ok) throw new Error("Failed to add follow-up");

      alert("Follow-up added successfully!");
    } catch (error) {
      alert(error.message);
    }
  };

  if (error) return <div className="text-red-500">{error}</div>;
  if (!leadData) return <div>Loading...</div>;

  return (
    <div className="flex-1 w-full overflow-hidden">
      <div className="text-left text-[18px] text-[#616161] font-rem font-normal mt-20">
        Home &gt; View Leads &gt; View Lead Info
      </div>

      <div className="mt-10 p-4 bg-white shadow-md rounded-lg font-rem w-full">
        <div className="flex items-center justify-between mt-2">
          <div className="text-[18px] font-rem font-normal">
            {/* {leadData.name} */}
          </div>
          <div className="flex items-center space-x-4 justify-end">
            {isEditing ? (
              <>
                <button
                  onClick={handleSaveClick}
                  className="bg-blue-500 text-white font-rem w-[85px] rounded px-3 py-2"
                >
                  Save
                </button>
                <button
                  className="bg-gray-300 text-black font-rem w-[85px] rounded px-3 py-2"
                  onClick={handleCancelEdit}
                >
                  Cancel
                </button>
              </>
            ) : (
              <>
                <button
                  onClick={() => setIsEditing(true)}
                  className="bg-green-500 text-white font-rem w-[85px] rounded px-3 py-2"
                >
                  Update
                </button>
                <button
                  onClick={() => setShowDeleteModal(true)}
                  className="bg-red-500 text-white font-rem w-[85px] rounded px-3 py-2"
                >
                  Delete
                </button>
              </>
            )}
          </div>
        </div>

        {/* Lead Information */}
        <div className="flex-col p-4 rounded-lg mt-4 text-left space-y-4 font-rem border-[#B1B1B1] border-[2px]">
          <p className="font-semibold">
            Name: <span className="text-gray-500">{leadData.name}</span>
          </p>
          <p className="font-semibold">
            Email: <span className="text-gray-500">{leadData.email}</span>
          </p>
          <p>
            Phone:
            {isEditing ? (
              <input
                type="text"
                value={newPhone}
                onChange={(e) => {
                  const value = e.target.value;
                  // Allow only numbers and limit to 10 digits
                  if (/^\d{0,10}$/.test(value)) {
                    setNewPhone(value);
                  }
                }}
                className="border p-1 rounded"
                placeholder="Enter 10-digit phone number"
              />
            ) : (
              <span className="text-[#979797]">{leadData.phone}</span>
            )}
          </p>

          <p className="font-semibold">
            Lead Source:{" "}
            <span className="text-gray-500">{leadData.leadSource}</span>
          </p>
          <div>
            Status:
            {isEditing ? (
              <select
                value={newStatus}
                onChange={(e) => {
                  console.log("Status changed to:", e.target.value); // Add this log
                  setNewStatus(e.target.value);
                }}
              >
                <option value="New">New</option>
                <option value="Contacted">Contacted</option>
                <option value="Follow-up">Follow-up</option>
                <option value="Converted">Converted</option>
                <option value="Lost">Lost</option>
              </select>
            ) : (
              <span className="ml-2 text-[#979797]">{leadData.status}</span>
            )}
          </div>
          <p className="font-semibold">
            Assigned To:{" "}
            <span className="text-gray-500">{leadData.assignedTo}</span>
          </p>
          <p className="font-semibold">
            Last Contacted:{" "}
            <span className="text-gray-500">{leadData.lastContacted}</span>
          </p>
          <p className="font-semibold">
            Location: <span className="text-gray-500">{leadData.location}</span>
          </p>
          <p className="font-semibold">
            Priority Level:{" "}
            <span className="text-gray-500">{leadData.priorityLevel}</span>
          </p>
          <p className="font-semibold">
            Description:{" "}
            <span className="text-gray-500">{leadData.description}</span>
          </p>

          {/* Added By */}
          <p className="font-semibold">Added By:</p>
          <div className="ml-4 border p-2 rounded-lg">
            <p className="font-semibold">
              Name:{" "}
              <span className="text-gray-500">{leadData.addedBy.name}</span>
            </p>
            <p className="font-semibold">
              Date:{" "}
              <span className="text-gray-500">{leadData.addedBy.date}</span>
            </p>
          </div>

          {/* Modified By */}
          <p className="font-semibold">Modified By:</p>
          <div className="ml-4 border p-2 rounded-lg">
            <p className="font-semibold">
              Name:{" "}
              <span className="text-gray-500">{leadData.modifiedBy.name}</span>
            </p>
            <p className="font-semibold">
              Date:{" "}
              <span className="text-gray-500">{leadData.modifiedBy.date}</span>
            </p>
          </div>

          {/* Notes Section */}
          <div className="text-left font-rem text-[18px] mt-4 font-semibold">
            Notes
          </div>
          <div className="space-y-3">
            {leadData.followUpNotes?.length > 0 ? (
              leadData.followUpNotes.map((note, index) => (
                <div
                  key={index}
                  className="p-4 rounded-lg border border-[#E0E0E0]"
                >
                  <p className="font-semibold">
                    Note: <span className="text-gray-500">{note.notes}</span>
                  </p>
                  <p className="font-semibold">
                    Follow-up Date:{" "}
                    <span className="text-gray-500">
                      {new Date(note.followUpDate).toLocaleString()}
                    </span>
                  </p>
                  <p className="font-semibold">
                    Added By:{" "}
                    <span className="text-gray-500">{note.addedBy}</span>
                  </p>
                </div>
              ))
            ) : (
              <p>No notes available for this lead.</p>
            )}
          </div>

          {/* Add Note Input */}
          {isEditing && (
            <div className="mt-4">
              <textarea
                className="w-full p-2 border rounded"
                rows="3"
                placeholder="Add a follow-up note..."
                value={newNotes}
                onChange={(e) => setNewNotes(e.target.value)}
              ></textarea>
            </div>
          )}

          {/* Follow-ups */}
          <div className=" p-4 rounded-lg mt-4 text-left space-y-4 font-rem border-[#B1B1B1] border-[2px]">
            <h2 className="text-lg font-bold mb-3">Add Follow-Up</h2>

            <div className="mb-3 w-64">
              <label className="block text-sm font-medium">
                Follow-Up Date
              </label>
              <input
                type="date"
                value={followUpDate}
                min={new Date().toISOString().split("T")[0]} // Set min date to today
                onChange={(e) => setFollowUpDate(e.target.value)}
                className="border p-2 rounded w-full"
              />
            </div>

            <div className="mb-3 w-64">
              <label className="block text-sm font-medium">Status</label>
              <select
                value={followUpStatus}
                onChange={(e) => setFollowUpStatus(e.target.value)}
                className="border p-2 rounded w-full"
              >
                {FollowUpStatusOptions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </div>

            <div className="mb-3">
              <label className="block text-sm font-medium">Notes</label>
              <textarea
                value={followUpNotes}
                onChange={(e) => setFollowUpNotes(e.target.value)}
                className="border p-2 rounded w-full"
                rows="3"
              ></textarea>
            </div>

            <button
              onClick={() =>
                handleAddFollowUp(followUpDate, followUpStatus, followUpNotes)
              }
              className="bg-blue-500 text-white px-4 py-2 rounded"
            >
              Add Follow-Up
            </button>
          </div>

          {/* Follow-Up History */}
          <div className="mt-6 p-4 border-[#B1B1B1] border-[2px] rounded-lg">
            <h2 className="text-lg font-bold mb-3">Follow-Up History</h2>

            {leadData.followUpNotes && leadData.followUpNotes.length > 0 ? (
              leadData.followUpNotes.map((followUp, index) => (
                <div
                  key={index}
                  className="border p-3 rounded-lg mb-2 bg-white"
                >
                  <p className="text-sm font-semibold">
                    Date:{" "}
                    <span className="text-gray-500">
                      {followUp.followUpDate}
                    </span>
                  </p>
                  <p className="text-sm font-semibold">
                    Status:{" "}
                    <span className="text-gray-500">{followUp.status}</span>
                  </p>
                  <p className="text-sm">
                    Notes:{" "}
                    <span className="text-gray-500">{followUp.notes}</span>
                  </p>
                </div>
              ))
            ) : (
              <p className="text-gray-500">No follow-ups recorded.</p>
            )}
          </div>

          {/* Confirmation Modal */}
          {showConfirmModal && (
            <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-50">
              <div className="bg-white p-6 rounded-lg shadow-lg w-96 text-center">
                <h2 className="text-lg font-bold">Confirm Update</h2>
                <p className="mt-2 text-gray-600">
                  Are you sure you want to update the lead details?
                </p>
                <div className="mt-4 flex justify-center space-x-4">
                  <button
                    className="bg-blue-500 text-white px-4 py-2 rounded-lg"
                    onClick={handleUpdateLead}
                  >
                    Yes, Update
                  </button>
                  <button
                    className="bg-gray-300 text-black px-4 py-2 rounded-lg"
                    onClick={() => setShowConfirmModal(false)}
                  >
                    Cancel
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Delete Confirmation Modal */}
          {showDeleteModal && (
            <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-50">
              <div className="bg-white p-6 rounded-lg shadow-lg w-96 text-center">
                <h2 className="text-lg font-bold">Confirm Deletion</h2>
                <p className="mt-2 text-gray-600">
                  Are you sure you want to delete this lead? This action cannot
                  be undone.
                </p>
                <div className="mt-4 flex justify-center space-x-4">
                  <button
                    className="bg-red-500 text-white px-4 py-2 rounded-lg"
                    onClick={handleDeleteLead}
                  >
                    Yes, Delete
                  </button>
                  <button
                    className="bg-gray-300 text-black px-4 py-2 rounded-lg"
                    onClick={() => setShowDeleteModal(false)}
                  >
                    Cancel
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ViewLeadInfo;
