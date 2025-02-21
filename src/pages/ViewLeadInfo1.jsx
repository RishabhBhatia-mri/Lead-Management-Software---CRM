import React, { useEffect, useState } from "react";
import { useParams, NavLink } from "react-router-dom";

const ViewLeadsInfo1 = () => {
  const statusOptions = ["New", "Contacted", "Follow-up", "Converted", "Lost"];

  const [isEditing, setisEditing] = useState(false);
  const [originalData, setOriginalData] = useState(leadData);
  const [newNotes, setNewNotes] = useState("");
  const [showModal, setShowModal] = useState(false);

  //   adding notes handler
  const handleAddNotes = () => {
    if (newNotes.trim()) {
      setLeadData({
        ...leadData,
        notes: [
          ...leadData.notes,
          {
            id: leadData.notes.length + 1,
            note: newNotes,
            date: new Date().toLocaleDateString(),
          },
        ],
      });
      setNewNotes("");
    }
  };

  const handleUpdateClick = () => {
    setisEditing(true);
  };

  const handleSaveClick = () => {
    setisEditing(false);
    // save the data to the backend that should be implemented below!
  };

  const handleDelete = () => {
    // delete the data from the backend that should be implemented below!
    console.log("Delete");
    setShowModal(false);
  };

  const Modal = ({ onClose, onDelete }) => {
    return (
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
        <div className="bg-white p-4 rounded-lg">
          <h2 className="text-lg font-semibold">
            Are you sure you want to delete?
          </h2>
          <div className="flex justify-end space-x-4 mt-4">
            <button
              className="bg-red-500 text-white px-4 py-2 rounded-lg"
              onClick={onDelete}
            >
              Delete
            </button>
            <button
              className="bg-gray-200 px-4 py-2 rounded-lg"
              onClick={onClose}
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    );
  };

  return (
    <div className="flex-auto min-h-screen w-[1094px]">
      <div className="text-left text-[18px] text-[#616161] font-rem font-normal mt-20">
        {" "}
        Home &gt; View Leads &gt; View Leads Info
      </div>

      <div className="mt-10 p-4 bg-white shadow-md rounded-lg font-rem w-full">
        <div className="flex items-center justify-between mt-2">
          <div className="text-[18px] font-rem font-normal">
            {leadData.name} - {leadData.id}
          </div>
          <div className="flex items-center space-x-4 justify-end">
            <button
              onClick={() => setisEditing(!isEditing)}
              className=" text-white  font-rem w-[85px]  "
            >
              {isEditing ? "Save" : "Update"}
            </button>
            <button
              className="font-rem bg-[#FF00003D] text-[#A90000] hover:bg-red-600 hover:text-white w-[85px] "
              onClick={() => {
                if (isEditing) {
                  setLeadData(originalData);
                  setisEditing(!isEditing);
                } else {
                  console.log("Delete");
                  setShowModal(true);
                }
              }}
            >
              {isEditing ? "Cancel" : "Delete"}
            </button>
          </div>
        </div>

        {/* Editable Information Box */}
        <div className="flex-col  p-4 rounded-lg mt-4 items-center text-left space-y-4 font-rem border-[#B1B1B1] border-[2px]">
          <p>
            Email:{" "}
            {isEditing ? (
              <input
                type="email"
                value={leadData.email}
                onChange={(e) =>
                  setLeadData({ ...leadData, email: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]">{leadData.email}</span>
            )}
          </p>
          <p>
            Phone:
            {isEditing ? (
              <input
                type="text"
                value={leadData.phone}
                onChange={(e) =>
                  setLeadData({ ...leadData, phone: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]">{leadData.phone}</span>
            )}
          </p>
          <p>
            Lead Source:
            {isEditing ? (
              <input
                type="text"
                value={leadData.leadSource}
                onChange={(e) =>
                  setLeadData({ ...leadData, leadSource: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]"> {leadData.leadSource}</span>
            )}
          </p>
          <div>
            Status:
            {isEditing ? (
              <select
                value={leadData.status}
                onChange={(e) =>
                  setLeadData({ ...leadData, status: e.target.value })
                }
                className="border ml-2 p-1 rounded"
              >
                {statusOptions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            ) : (
              <span className="ml-2 text-[#979797]">{leadData.status}</span>
            )}
          </div>
          <p>
            Assigned To:
            {isEditing ? (
              <input
                type="text"
                value={leadData.assignedTo}
                onChange={(e) =>
                  setLeadData({ ...leadData, assignedTo: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]"> {leadData.assignedTo}</span>
            )}
          </p>
          <p>
            Last Contacted:
            {isEditing ? (
              <input
                type="text"
                value={leadData.lastContacted}
                onChange={(e) =>
                  setLeadData({ ...leadData, lastContacted: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]"> {leadData.lastContacted}</span>
            )}
          </p>
        </div>

        {/* detailed info */}
        <div className="text-left font-rem font-[18px] mt-8">Detailed Info</div>
        <div className="flex-col  p-4 rounded-lg mt-4 items-center text-left space-y-4 font-rem border-[#B1B1B1] border-[2px]">
          <p>
            Full Name:{" "}
            {isEditing ? (
              <input
                type="text"
                value={leadData.fullName}
                onChange={(e) =>
                  setLeadData({ ...leadData, fullName: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]">{leadData.fullName}</span>
            )}
          </p>
          <p>
            Email:{" "}
            {isEditing ? (
              <input
                type="email"
                value={leadData.email}
                onChange={(e) =>
                  setLeadData({ ...leadData, email: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]">{leadData.email}</span>
            )}
          </p>
          <p>
            Phone:
            {isEditing ? (
              <input
                type="text"
                value={leadData.phone}
                onChange={(e) =>
                  setLeadData({ ...leadData, phone: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]">{leadData.phone}</span>
            )}
          </p>
          <p>
            Company Name:
            {isEditing ? (
              <input
                type="text"
                value={leadData.companyName}
                onChange={(e) =>
                  setLeadData({ ...leadData, companyName: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]"> {leadData.companyName}</span>
            )}
          </p>
          <p>
            Location:
            {isEditing ? (
              <input
                type="text"
                value={leadData.location}
                onChange={(e) =>
                  setLeadData({ ...leadData, location: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]"> {leadData.location}</span>
            )}
          </p>
          <p>
            Lead Source:
            {isEditing ? (
              <input
                type="text"
                value={leadData.leadSource}
                onChange={(e) =>
                  setLeadData({ ...leadData, leadSource: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]"> {leadData.leadSource}</span>
            )}
          </p>
          <div>
            Status:
            {isEditing ? (
              <select
                value={leadData.status}
                onChange={(e) =>
                  setLeadData({ ...leadData, status: e.target.value })
                }
                className="border ml-2 p-1 rounded"
              >
                {statusOptions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            ) : (
              <span className="ml-2 text-[#979797]">{leadData.status}</span>
            )}
          </div>
          <p>
            Priority Level:
            {isEditing ? (
              <input
                type="text"
                value={leadData.priorityLevel}
                onChange={(e) =>
                  setLeadData({ ...leadData, priorityLevel: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]"> {leadData.priorityLevel}</span>
            )}
          </p>
          <p>
            Assigned Sales Rep:
            {isEditing ? (
              <input
                type="text"
                value={leadData.assignedTo}
                onChange={(e) =>
                  setLeadData({ ...leadData, assignedTo: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]"> {leadData.assignedTo}</span>
            )}
          </p>
          <p>
            Added By:
            <span className="text-[#979797]">
              {leadData.addedBy.name} {leadData.addedBy.date}
            </span>
          </p>
          <p>
            Modified By:
            <span className="text-[#979797]">
              {" "}
              {leadData.modifiedBy.name} {leadData.modifiedBy.date}
            </span>
          </p>
          <p>
            Description:
            {isEditing ? (
              <input
                type="text"
                value={leadData.description}
                onChange={(e) =>
                  setLeadData({ ...leadData, description: e.target.value })
                }
                className="border p-1 rounded"
              />
            ) : (
              <span className="text-[#979797]"> {leadData.description}</span>
            )}
          </p>
        </div>
        <div className="flex-col  p-4 rounded-lg mt-4 items-center text-left space-y-4 font-rem border-[#B1B1B1] border-[2px]">
          <div className="text-left font-rem text-[18px] mt-4 ">Notes</div>
          <div className="flex gap-2 mt-4">
            <input
              type="text"
              placeholder="Add new note..."
              value={newNotes}
              onChange={(e) => setNewNotes(e.target.value)}
              className="flex-1 border-2 border-[#B1B1B1] p-2 rounded-lg"
            />
            <button
              onClick={handleAddNotes}
              className="text-white px-4 py-2 rounded-lg "
            >
              Add Note
            </button>
          </div>
          <div className="space-y-3">
            {leadData.notes.map((note) => (
              <div
                key={note.id}
                className="p-3 rounded-lg border border-[#E0E0E0]"
              >
                <p className="text-[#616161]">{note.note}</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default ViewLeadsInfo1;
