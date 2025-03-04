import React, { useEffect, useState, useRef } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader";
import * as THREE from "three";
import axios from "axios";
import "leaflet/dist/leaflet.css";
import { MapContainer, TileLayer, Rectangle, useMapEvents } from "react-leaflet";

// REMOVE THIS (it causes a circular import)
// import HomePage from './HomePage';

const Model = ({ modelUrl, setCameraPosition }) => {
  const [model, setModel] = useState(null);

  useEffect(() => {
    if (!modelUrl) return;

    const loader = new GLTFLoader();
    loader.load(
      modelUrl,
      (gltf) => {
        const scene = gltf.scene;
        console.log("Loaded model:", scene);
        setModel(scene);

        // Compute bounding box
        const box = new THREE.Box3().setFromObject(scene);
        const center = new THREE.Vector3();
        box.getCenter(center);
        const size = box.getSize(new THREE.Vector3());

        // Move model up
        scene.position.y -= box.min.y;

        // Normalize scale
        const maxDim = Math.max(size.x, size.y, size.z);
        const scaleFactor = 5 / maxDim;
        scene.scale.set(scaleFactor, scaleFactor, scaleFactor);

        // Adjust camera
        const newCameraPosition = [center.x, center.y + size.y * 1.5, center.z + size.z * 2];
        setCameraPosition(newCameraPosition);
      },
      undefined,
      (error) => console.error("Error loading model:", error)
    );
  }, [modelUrl, setCameraPosition]);

  return model ? <primitive object={model} /> : null;
};

const HomePage = () => {
  const [modelUrl, setModelUrl] = useState(null);
  const [loading, setLoading] = useState(false);
  const [boundingBox, setBoundingBox] = useState([
    [49.2, 18.6], // Default coords (Top-left)
    [49.3, 18.7], // Default coords (Bottom-right)
  ]);
  const controlsRef = useRef();
  const [cameraPosition, setCameraPosition] = useState([0, 5, 15]);

  // Function to send API request for model generation
  const generateModel = async () => {
    setLoading(true);
    try {
      const response = await axios.post("https://localhost:7188/api/generate-model", {
        boundingBox, // Send selected map area
      });

      console.log("API Response:", response.data);

      if (response.data && response.data.fileUrl) {
        setModelUrl(`https://localhost:7188${response.data.fileUrl}`); // Update model URL
      } else {
        console.error("Model URL not found in response");
      }
    } catch (error) {
      console.error("Error fetching model URL:", error);
    }
    setLoading(false);
  };

  return (
    <div style={{ width: "100vw", height: "100vh", display: "flex", flexDirection: "column" }}>
      {/* Map Section */}
      <div style={{ flex: 1, position: "relative" }}>
        <h2 style={{ textAlign: "center" }}>Select an area for 3D model</h2>
        <MapContainer center={[49.2, 18.6]} zoom={13} style={{ width: "100%", height: "100%" }}>
          <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
          <Rectangle bounds={boundingBox} color="blue" />
          <MapEvents setBoundingBox={setBoundingBox} />
        </MapContainer>
      </div>

      {/* Generate Model Button */}
      <button
        onClick={generateModel}
        disabled={loading}
        style={{ margin: "10px auto", padding: "10px 20px", fontSize: "16px" }}
      >
        {loading ? "Generating..." : "Generate 3D Model"}
      </button>

      {/* 3D Model Section */}
      <div style={{ flex: 1, background: "#f0f0f0" }}>
        <h2 style={{ textAlign: "center" }}>3D Model Preview</h2>
        <Canvas camera={{ position: cameraPosition, fov: 50 }}>
          <ambientLight intensity={1} />
          <directionalLight position={[10, 20, 10]} intensity={1.5} />
          <axesHelper args={[5]} />
          <gridHelper args={[10, 10]} />
          {modelUrl && <Model modelUrl={modelUrl} setCameraPosition={setCameraPosition} />}
          <OrbitControls
            ref={controlsRef}
            minDistance={2}
            maxDistance={100}
            enableDamping={true}
            dampingFactor={0.1}
            rotateSpeed={0.5}
          />
        </Canvas>
      </div>
    </div>
  );
};

// Component to handle map clicks & update bounding box
const MapEvents = ({ setBoundingBox }) => {
  useMapEvents({
    click(e) {
      const lat = e.latlng.lat;
      const lng = e.latlng.lng;
      setBoundingBox([
        [lat - 0.05, lng - 0.05],
        [lat + 0.05, lng + 0.05],
      ]);
    },
  });
  return null;
};

export default HomePage;
