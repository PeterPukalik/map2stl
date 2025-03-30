// UserModelList.jsx
import React, { useState, useEffect } from "react";
import * as signalR from "@microsoft/signalr";
import { 
  fetchUserModels, 
  downloadModel, 
  shareModel, 
  createStlFromModel,
  BASE_URL
} from "../../services/api";
import ModelViewerBlob from "../MapComponents/ModelViewBlob";
import ModelViewerStl from "../MapComponents/ModelViewerStl";

const UserModelList = () => {
  const [models, setModels] = useState([]);
  const [selectedModelId, setSelectedModelId] = useState(null);
  const [viewerUrl, setViewerUrl] = useState(null);
  const [shareLink, setShareLink] = useState("");
  const [error, setError] = useState("");
  const [progressMessages, setProgressMessages] = useState([]);
  const [isConverting, setIsConverting] = useState(false);
  const [connection, setConnection] = useState(null);
  const [connectionId, setConnectionId] = useState("");
  const [viewerFormat, setViewerFormat] = useState("");

  // Establish SignalR connection on mount.
  useEffect(() => {
    console.log("base url is:" + BASE_URL);
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE_URL}/progressHub`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    newConnection.start()
      .then(async () => {
        const cid = await newConnection.invoke("GetConnectionId");
        setConnectionId(cid);
        console.log("Connection ID from hub:", cid);
      })
      .catch(err => console.error("SignalR connection error:", err));

    newConnection.on("ReceiveProgress", (message) => {
      setProgressMessages(prev => [...prev, message]);
    });

    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, [BASE_URL]);

  // Fetch models when component mounts.
  useEffect(() => {
    async function getModels() {
      try {
        const response = await fetchUserModels();
        setModels(response || []);
      } catch (err) {
        console.error(err);
        setError("Failed to load models.");
      }
    }
    getModels();
  }, []);

// When viewing GLTF:
const handleViewGltf = async (modelId) => {
  try {
    const blob = await downloadModel(modelId, "glb");
    const url = URL.createObjectURL(blob);
    setViewerUrl(url);
    setSelectedModelId(modelId);
    setViewerFormat("glb");
  } catch (err) {
    console.error(err);
    setError(`Error viewing GLTF model ${modelId}`);
  }
};

// When viewing STL:
const handleViewStl = async (modelId) => {
  try {
    const blob = await downloadModel(modelId, "stl");
    const url = URL.createObjectURL(blob);
    setViewerUrl(url);
    setSelectedModelId(modelId);
    setViewerFormat("stl");
  } catch (err) {
    console.error(err);
    setError(`Error viewing STL model ${modelId}`);
  }
};

  // Action: Generate STL from a model.
  const handleGenerateStl = async (modelId) => {
    if (!connectionId) {
      console.error("SignalR connection ID not available.");
      return;
    }
    setProgressMessages([]);
    setIsConverting(true);

    // Create an object with override parameters. Adjust these as needed.
    const requestBody = {
      IncludeBuildings: true,
      ZFactor: 1,
      MeshReduceFactor: 1,
      BasePlateHeightFraction: 0.25,
      BasePlateOffset: 0
    };

    try {
      const token = localStorage.getItem("token");
      await createStlFromModel(modelId, connectionId, requestBody, token);
      console.log("STL generation initiated.");
    } catch (err) {
      console.error("Error initiating STL conversion:", err);
      setError("Error initiating STL conversion.");
      setIsConverting(false);
    }
  };

  // Action: Download STL.
  const handleDownload = async (modelId) => {
    try {
      const blob = await downloadModel(modelId, "stl");
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `model_${modelId}.stl`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error(err);
      setError(`Error downloading model ${modelId}`);
    }
  };

  // Action: Generate a shareable link.
  const handleShare = async (modelId) => {
    try {
      const result = await shareModel(modelId);
      setShareLink(result.shareLink);
      alert(`Share link generated: ${result.shareLink}`);
    } catch (err) {
      console.error(err);
      setError(`Error generating share link for model ${modelId}`);
    }
  };

  return (
    <div style={{ padding: "1rem" }}>
      <h2>User Models</h2>
      {error && <p style={{ color: "red" }}>{error}</p>}
      {models.length > 0 ? (
        <table border="1" cellPadding="5" cellSpacing="0" style={{ width: "100%" }}>
          <thead>
            <tr>
              <th>Model Name</th>
              <th>Description</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {models.map((model) => (
              <tr key={model.id}>
                <td>{model.name}</td>
                <td>{model.description}</td>
                <td>
                  <button onClick={() => handleViewGltf(model.id)} style={{ marginLeft: "8px" }}>
                    View GLTF
                  </button>
                  <button onClick={() => handleViewStl(model.id)} style={{ marginLeft: "8px" }}>
                    View STL
                  </button>
                  <button onClick={() => handleGenerateStl(model.id)} style={{ marginLeft: "8px" }}>
                    Generate STL
                  </button>
                  <button onClick={() => handleDownload(model.id)} style={{ marginLeft: "8px" }}>
                    Download STL
                  </button>
                  <button onClick={() => handleShare(model.id)} style={{ marginLeft: "8px" }}>
                    Share
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <p>No models found.</p>
      )}

      {/* Display the progress messages with a dark background and white text */}
      {isConverting && (
        <div
          style={{
            marginTop: "1rem",
            padding: "1rem",
            backgroundColor: "#222",
            color: "#fff",
            borderRadius: "5px",
            maxHeight: "300px",
            overflowY: "auto"
          }}
        >
          <h3 style={{ marginTop: 0 }}>Conversion Progress:</h3>
          {progressMessages.length === 0 ? (
            <p>No progress yet...</p>
          ) : (
            <ul style={{ margin: 0, paddingLeft: "1rem" }}>
              {progressMessages.map((msg, idx) => (
                <li key={idx}>{msg}</li>
              ))}
            </ul>
          )}
        </div>
      )}

      {/* Display the 3D viewer if a model is selected and viewerUrl is set */}
      {selectedModelId && viewerUrl && viewerFormat === "glb" && (
                  <div style={{ marginTop: "2rem" }}>
                    <h3>Viewing GLTF Model {selectedModelId}</h3>
                    <ModelViewerBlob modelSource={viewerUrl} />
                  </div>
                )}

                {selectedModelId && viewerUrl && viewerFormat === "stl" && (
                  <div style={{ marginTop: "2rem" }}>
                    <h3>Viewing STL Model {selectedModelId}</h3>
                    <ModelViewerStl modelSource={viewerUrl} />
                  </div>
                )}


      {/* Optionally display the share link */}
      {shareLink && (
        <div style={{ marginTop: "1rem" }}>
          <p>
            Share this link:{" "}
            <a href={shareLink} target="_blank" rel="noopener noreferrer">
              {shareLink}
            </a>
          </p>
        </div>
      )}
    </div>
  );
};

export default UserModelList;
