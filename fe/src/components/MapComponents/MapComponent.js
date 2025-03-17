import React, { useState } from "react";
import { MapContainer, TileLayer, FeatureGroup } from "react-leaflet";
import { EditControl } from "react-leaflet-draw";
import { generateTerrainModel } from "../../services/api";
import GltfViewer from "./ModelViewer";
import "leaflet/dist/leaflet.css";
import "leaflet-draw/dist/leaflet.draw.css";
import "./MapComponent.css"; 

function MapWithDraw() {
  const [bounds, setBounds] = useState(null);
  const [modelUrl, setModelUrl] = useState(null);
  const [loading, setLoading] = useState(false);

  // Sliders
  const [zFactor, setZFactor] = useState(1);       // range 1..10
  const [meshReduce, setMeshReduce] = useState(0.5); // range 0..1

  // When a rectangle is created on the map
  const onCreated = (e) => {
    const { layerType, layer } = e;
    if (layerType === "rectangle") {
      const rectangleBounds = layer.getBounds();
      const sw = rectangleBounds.getSouthWest();
      const ne = rectangleBounds.getNorthEast();
      setBounds({
        SouthLat: sw.lat,
        WestLng: sw.lng,
        NorthLat: ne.lat,
        EastLng: ne.lng,
      });
    }
  };

  // Generate the 3D model via API
  const sendToBackend = async () => {
    if (!bounds) return;
    setLoading(true);

    try {
      const response = await generateTerrainModel({
        ...bounds,
        zFactor,
        meshReduce,
      });
      console.log("API Response:", response);

      if (typeof response === "string") {
        setModelUrl(response);
      } else if (response && response.fileUrl) {
        const fullUrl = `https://localhost:7188${response.fileUrl}`;
        setModelUrl(fullUrl);
      } else {
        console.error("Unexpected API response format:", response);
      }
    } catch (err) {
      console.error("Error fetching model URL:", err);
    }
    setLoading(false);
  };

  return (
    <div className="outer-wrapper">
      {/* TOP SECTION: Controls (left) + Map (right) */}
      <div className="top-section">
        <div className="controls-column">
          <h3>Model Settings</h3>

          <div className="slider-block">
            <label>Z Factor: {zFactor}</label>
            <input
              type="range"
              min="1"
              max="10"
              step="1"
              value={zFactor}
              onChange={(e) => setZFactor(Number(e.target.value))}
            />
          </div>

          <div className="slider-block">
            <label>Mesh Reduction: {meshReduce}</label>
            <input
              type="range"
              min="0"
              max="1"
              step="0.1"
              value={meshReduce}
              onChange={(e) => setMeshReduce(Number(e.target.value))}
            />
          </div>

          <button className="generate-button" onClick={sendToBackend} disabled={!bounds || loading}>
            {loading ? "Generating..." : "Generate 3D Model"}
          </button>
        </div>

        <div className="map-column">
          <MapContainer
            center={[48.673, 19.699]}
            zoom={7}
            scrollWheelZoom={true}
            className="map-container"
          >
            <TileLayer
              attribution="&copy; OpenStreetMap contributors"
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            <FeatureGroup>
              <EditControl
                position="topright"
                draw={{
                  polyline: false,
                  polygon: false,
                  circle: false,
                  circlemarker: false,
                  marker: false,
                  rectangle: true,
                }}
                edit={{ remove: true }}
                onCreated={onCreated}
              />
            </FeatureGroup>
          </MapContainer>
        </div>
      </div>

      {/* BOTTOM SECTION: 3D Model Preview */}
      <div className="bottom-section">
        {modelUrl ? (
          <GltfViewer modelUrl={modelUrl} />
        ) : (
          <h3>3D Model will appear here</h3>
        )}
      </div>
    </div>
  );
}

export default MapWithDraw;
