import React, { useEffect, useState } from 'react';
import { getWeatherData } from '../api'; // Corrected import path

const MyComponent = () => {
  const [weatherData, setWeatherData] = useState([]);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const data = await getWeatherData();
        setWeatherData(data);
      } catch (error) {
        console.error('Error loading weather data:', error);
      }
    };

    fetchData();
  }, []);

  return (
    <div>
      <h1>Weather Forecast</h1>
      {weatherData.map((item, index) => (
        <div key={index}>
          <p>Date: {item.date}</p>
          <p>Temperature (C): {item.temperatureC}</p>
          <p>Summary: {item.summary}</p>
        </div>
      ))}
    </div>
  );
};

export default MyComponent;
