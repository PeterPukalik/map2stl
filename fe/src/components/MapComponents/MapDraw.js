const sendToBackend = async () => {
    if (!bounds) return;
  
    try {
      const modelUrl = await generateTerrainModel(bounds);
      setModelUrl(modelUrl);
      alert("3D Model successfully generated!");
    } catch (err) {
      alert(`Error generating model: ${err.message}`);
    }
  };
  