import React from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader";
import { useLoader } from "@react-three/fiber";



const GltfViewer = ({ modelUrl }) => {
  const gltf = useLoader(GLTFLoader, modelUrl);

  return (
    <Canvas camera={{ position: [0, 2, 5] }}>
      <ambientLight intensity={0.5} />
      <pointLight position={[10, 10, 10]} />
      <primitive object={gltf.scene} />
      <OrbitControls />
    </Canvas>
  );
};

export default GltfViewer;
