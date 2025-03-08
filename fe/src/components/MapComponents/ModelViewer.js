import React, { useEffect, useState, useRef } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader";
import * as THREE from "three";

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

        // Move model up so it doesn't sink
        scene.position.y -= box.min.y;

        // Normalize scale
        const maxDim = Math.max(size.x, size.y, size.z);
        const scaleFactor = 5 / maxDim;
        scene.scale.set(scaleFactor, scaleFactor, scaleFactor);

        // Adjust camera
        const newCameraPosition = [
          center.x,
          center.y + size.y * 1.5,
          center.z + size.z * 2,
        ];
        setCameraPosition(newCameraPosition);
      },
      undefined,
      (error) => console.error("Error loading model:", error)
    );
  }, [modelUrl, setCameraPosition]);

  return model ? <primitive object={model} /> : null;
};

const ModelViewver = ({ modelUrl }) => {
  const controlsRef = useRef();
  const [cameraPosition, setCameraPosition] = useState([0, 5, 15]);

  return (
    <div style={{ width: "100%", height: "100%", background: "#f0f0f0" }}>
      <Canvas camera={{ position: cameraPosition, fov: 50 }}>
        {/* Lights */}
        <ambientLight intensity={1} />
        <directionalLight position={[10, 20, 10]} intensity={1.5} />

        {/* Load the model dynamically */}
        {modelUrl ? (
          <Model modelUrl={modelUrl} setCameraPosition={setCameraPosition} />
        ) : (
          <mesh>
            <boxGeometry args={[1, 1, 1]} />
            <meshStandardMaterial color="gray" />
          </mesh>
        )}

        {/* Orbit Controls for rotation and zooming */}
        <OrbitControls
          ref={controlsRef}
          minDistance={0}
          maxDistance={100}
          enableDamping={true}
          dampingFactor={0.1}
          rotateSpeed={0.5}
        />
      </Canvas>
    </div>
  );
};

export default ModelViewver;
