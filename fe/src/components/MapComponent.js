import React, { useState } from 'react';
import { MapContainer, TileLayer, Rectangle, useMapEvents } from 'react-leaflet';

const MapComponent = ({ onAreaSelect }) => {
  const [bounds, setBounds] = useState(null);

  // Function to handle map clicks and area selection
  const MapEvents = () => {
    useMapEvents({
      click(event) {
        const { lat, lng } = event.latlng;
        // Example: Set a rectangular selection area
        const selectedBounds = [
          [lat - 0.01, lng - 0.01], // Bottom-left corner
          [lat + 0.01, lng + 0.01], // Top-right corner
        ];
        setBounds(selectedBounds);
        onAreaSelect(selectedBounds); // Pass bounds to parent component
      },
    });
    return null;
  };

  return (
    <MapContainer center={[51.505, -0.09]} zoom={13k} style={{ height: '400px', width: '50%' }}>
      <TileLayer
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        attribution="&copy; OpenStreetMap contributors"
      />
      {bounds && <Rectangle bounds={bounds} />}
      <MapEvents />
    </MapContainer>
  );
};

export default MapComponent;
