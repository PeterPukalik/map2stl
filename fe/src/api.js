import axios from 'axios';

const API_BASE_URL = 'https://localhost:7188/WeatherForecast';

export const getWeatherData = async () => {
  try {
    const response = await axios.get(API_BASE_URL);
    return response.data;
  } catch (error) {
    console.error('Error fetching weather data:', error);
    throw error;
  }
};
