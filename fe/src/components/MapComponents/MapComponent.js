import React, { useState } from "react";
import { MapContainer, TileLayer, FeatureGroup } from "react-leaflet";
import { EditControl } from "react-leaflet-draw";
import "leaflet/dist/leaflet.css";
import "leaflet-draw/dist/leaflet.draw.css";
import { generateTerrainModel } from "../../services/api";
import GltfViewer from "./ModelViewer"; // 3D Viewer Component
import "./MapComponent.css";

function MapWithDraw() {
  const [bounds, setBounds] = useState(null);
  const [modelUrl, setModelUrl] = useState(null);
  const [loading, setLoading] = useState(false);

  // Handler for when a shape is created
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

// API Call to Generate Model
const sendToBackend = async () => {
  if (!bounds) return;
  setLoading(true);

  try {
    const response = await generateTerrainModel(bounds);

    console.log("API Response:", response); // Debugging

    // If response is a string, use it directly
    if (typeof response === "string") {
      setModelUrl(response); // API is returning a full URL, so no need to append anything
    } 
    // If response is an object, use fileUrl
    else if (response && response.fileUrl) {
      const fullUrl = `https://localhost:7188${response.fileUrl}`;
      setModelUrl(fullUrl);
    } 
    // If neither, log an error
    else {
      console.error("Unexpected API response format:", response);
    }
  } catch (err) {
    console.error("Error fetching model URL:", err);
  }

  setLoading(false);
};


  return (
    <div className="wrapper">
      {/* MAP BOX */}
      <div className="box">
        <MapContainer className="map-container" center={[48.673, 19.699]} zoom={7} scrollWheelZoom={true}>
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

      {/* BUTTON + STATUS TEXT */}
      <div className="controls">
        <button onClick={sendToBackend} disabled={!bounds || loading}>
          {loading ? "Generating..." : "Generate 3D Model"}
        </button>
      </div>

      {/* 3D MODEL BOX */}
      <div className="box">
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


//mena modelov
//email
//db modelov, use case, class diagram
//zdielanie modelov -cez odkaz
//popisat kniznice, frameworky, preco oracle
//zaver = zhodnotenie, co zlepsit
//systemova,programatorska,uzivatelska
//kazdu stredu o 7