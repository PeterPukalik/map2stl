// UserModelList.js
import React, { useState, useEffect } from "react";
import { fetchUserModels, downloadModel, shareModel } from "../../services/api";
import ModelViewerBlob from "../MapComponents/ModelViewBlob";

const UserModelList = () => {
  const [models, setModels] = useState([]);
  const [selectedModelId, setSelectedModelId] = useState(null);
  const [viewerUrl, setViewerUrl] = useState(null); // URL to view the model (GLB)
  const [downloadUrl, setDownloadUrl] = useState(null); // URL to download the model (STL)
  const [shareLink, setShareLink] = useState("");
  const [error, setError] = useState("");

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

  // Action: View the model (GLB)
  const handleView = async (modelId) => {
    try {
      // Call downloadModel with format "glb" to fetch the model as a Blob.
      const blob = await downloadModel(modelId, "glb");
      // Create an object URL from the Blob.
      const url = URL.createObjectURL(blob);
      setViewerUrl(url);
      setSelectedModelId(modelId);
    } catch (err) {
      console.error(err);
      setError(`Error viewing model ${modelId}`);
    }
  };


  // Action: Download the model (STL)
  const handleDownload = async (modelId) => {
    try {
      // Call downloadModel with format "glb" for download.
      const blob = await downloadModel(modelId, "glb");
      // Create an object URL from the Blob.
      const url = URL.createObjectURL(blob);
      // Create a temporary <a> element and set its download attribute.
      const a = document.createElement("a");
      a.href = url;
      a.download = `model_${modelId}.glb`;
      document.body.appendChild(a);
      a.click();
      // Clean up by removing the anchor and revoking the object URL.
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error(err);
      setError(`Error downloading model ${modelId}`);
    }
  };
  

  // Action: Generate a shareable link for the model
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
    <div>
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
                  <button onClick={() => handleView(model.id)}>View</button>
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

      {/* Display the 3D viewer if a model is selected and viewerUrl is set */}
      {selectedModelId && viewerUrl && (
        <div style={{ marginTop: "2rem" }}>
          <h3>Viewing Model {selectedModelId}</h3>
          <ModelViewerBlob modelSource={viewerUrl} />
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
