import React, { useState } from "react";
import { MapContainer, TileLayer, FeatureGroup } from "react-leaflet";
import { EditControl } from "react-leaflet-draw";
import "leaflet/dist/leaflet.css";
import "leaflet-draw/dist/leaflet.draw.css";
import { generateTerrainGif } from "../../services/api";
import "./MapComponent.css";

function MapWithDraw() {
  const [bounds, setBounds] = useState(null);

  // Handler for when a shape is created
  const onCreated = (e) => {
    const { layerType, layer } = e;

    if (layerType === "rectangle") {
      const rectangleBounds = layer.getBounds();
      const sw = rectangleBounds.getSouthWest();
      const ne = rectangleBounds.getNorthEast();

      // Save latlng corners in state
      setBounds({
        SouthLat: sw.lat,
        WestLng: sw.lng,
        NorthLat: ne.lat,
        EastLng: ne.lng,
      });
    }
  };

  const sendToBackend = async () => {
    if (!bounds) return;

    try {
      const response = await generateTerrainGif(bounds);
      alert("Terrain GIF successfully generated!");
      console.log("Backend response:", response);
    } catch (err) {
      alert(`Error generating terrain GIF: ${err.message}`);
    }
  };

  return (
    <div>
      <MapContainer className="map-container" center={[48.673, 19.699]} zoom={7}>
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
            edit={{
              remove: true,
            }}
            onCreated={onCreated}
          />
        </FeatureGroup>
      </MapContainer>

      <button onClick={sendToBackend} disabled={!bounds}>
        Generate GIF
      </button>

      {bounds && (
        <p>
          Southwest: ({bounds.SouthLat.toFixed(4)}, {bounds.WestLng.toFixed(4)})<br />
          Northeast: ({bounds.NorthLat.toFixed(4)}, {bounds.EastLng.toFixed(4)})
        </p>
      )}
    </div>
  );
}

export default MapWithDraw;
