using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Generates mobile application code based on requirements.
/// </summary>
public class MobileAppCodeGenerator
{
    public string GenerateMobileAppCode(string prompt, List<string> features)
    {
        var baseCode = @"import React, { useState, useEffect } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { StatusBar } from 'expo-status-bar';

export default function App() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      setData([{ id: 1, title: 'Sample Item', description: 'This is a sample item' }]);
    } catch (error) {
      Alert.alert('Error', 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <StatusBar style=""auto"" />
      <View style={styles.header}>
        <Text style={styles.title}>Mobile App</Text>
        <Text style={styles.subtitle}>Generated with React Native</Text>
      </View>
      
      <ScrollView style={styles.content}>
        <Text style={styles.sectionTitle}>Features</Text>
        {FEATURES}
        
        <Text style={styles.sectionTitle}>App Details</Text>
        <Text style={styles.description}>Prompt: {PROMPT}</Text>
        <Text style={styles.description}>Features: {FEATURE_LIST}</Text>
        
        <TouchableOpacity style={styles.button} onPress={loadData}>
          <Text style={styles.buttonText}>Refresh Data</Text>
        </TouchableOpacity>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  header: {
    backgroundColor: '#007bff',
    padding: 20,
    paddingTop: 50,
    alignItems: 'center',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: 'white',
  },
  subtitle: {
    fontSize: 16,
    color: 'white',
    marginTop: 5,
  },
  content: {
    flex: 1,
    padding: 20,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    marginTop: 20,
    marginBottom: 10,
  },
  description: {
    fontSize: 14,
    color: '#666',
    marginBottom: 10,
  },
  button: {
    backgroundColor: '#007bff',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
    marginTop: 20,
  },
  buttonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: 'bold',
  },
  featureCard: {
    backgroundColor: 'white',
    padding: 15,
    borderRadius: 8,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  featureTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    marginBottom: 5,
  },
  featureDescription: {
    fontSize: 14,
    color: '#666',
  },
});

// Additional functionality based on features
{JAVASCRIPT_CODE}";

        var featureCards = features.Select(feature => $@"
        <View style={styles.featureCard}>
          <Text style={styles.featureTitle}>{GetFeatureIcon(feature)} {feature}</Text>
          <Text style={styles.featureDescription}>Implemented with React Native</Text>
        </View>").ToList();

        var javascriptCode = GenerateJavaScriptCode(features);

        return baseCode
            .Replace("{FEATURES}", string.Join("", featureCards))
            .Replace("{PROMPT}", prompt)
            .Replace("{FEATURE_LIST}", string.Join(", ", features))
            .Replace("{JAVASCRIPT_CODE}", javascriptCode);
    }

    private string GetFeatureIcon(string feature)
    {
        return feature.ToLower() switch
        {
            "mobile" => "📱",
            "charts" => "📊",
            "social" => "👥",
            "offline" => "📶",
            "camera" => "📷",
            "location" => "📍",
            "notifications" => "🔔",
            "biometrics" => "👆",
            _ => "✨"
        };
    }

    private string GenerateJavaScriptCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("Charts"))
        {
            code.Add(@"
// Chart functionality
import { LineChart, BarChart } from 'react-native-chart-kit';

const ChartComponent = ({ data }) => {
  return (
    <LineChart
      data={data}
      width={300}
      height={220}
      chartConfig={{
        backgroundColor: '#e26a00',
        backgroundGradientFrom: '#fb8c00',
        backgroundGradientTo: '#ffa726',
        decimalPlaces: 2,
        color: (opacity = 1) => `rgba(255, 255, 255, ${opacity})`,
        style: {
          borderRadius: 16
        }
      }}
    />
  );
};");
        }

        if (features.Contains("Social"))
        {
            code.Add(@"
// Social functionality
const shareContent = async (content) => {
  try {
    await Share.share({
      message: content,
    });
  } catch (error) {
    console.error('Error sharing:', error);
  }
};

const connectSocial = async (platform) => {
  try {
    // Implement social media integration
    console.log('Connecting to', platform);
  } catch (error) {
    console.error('Error connecting to social platform:', error);
  }
};");
        }

        if (features.Contains("Offline"))
        {
            code.Add(@"
// Offline functionality
import NetInfo from '@react-native-community/netinfo';

const useOfflineSupport = () => {
  const [isConnected, setIsConnected] = useState(true);

  useEffect(() => {
    const unsubscribe = NetInfo.addEventListener(state => {
      setIsConnected(state.isConnected);
    });

    return () => unsubscribe();
  }, []);

  return isConnected;
};");
        }

        if (features.Contains("Camera"))
        {
            code.Add(@"
// Camera functionality
import { Camera } from 'expo-camera';

const CameraComponent = () => {
  const [hasPermission, setHasPermission] = useState(null);
  const [cameraRef, setCameraRef] = useState(null);

  useEffect(() => {
    (async () => {
      const { status } = await Camera.requestPermissionsAsync();
      setHasPermission(status === 'granted');
    })();
  }, []);

  const takePicture = async () => {
    if (cameraRef) {
      const photo = await cameraRef.takePictureAsync();
      console.log('Photo taken:', photo.uri);
    }
  };

  return (
    <Camera
      style={styles.camera}
      type={Camera.Constants.Type.back}
      ref={setCameraRef}
    >
      <TouchableOpacity onPress={takePicture}>
        <Text>Take Picture</Text>
      </TouchableOpacity>
    </Camera>
  );
};");
        }

        if (features.Contains("Location"))
        {
            code.Add(@"
// Location functionality
import * as Location from 'expo-location';

const useLocation = () => {
  const [location, setLocation] = useState(null);
  const [errorMsg, setErrorMsg] = useState(null);

  useEffect(() => {
    (async () => {
      let { status } = await Location.requestForegroundPermissionsAsync();
      if (status !== 'granted') {
        setErrorMsg('Permission to access location was denied');
        return;
      }

      let location = await Location.getCurrentPositionAsync({});
      setLocation(location);
    })();
  }, []);

  return { location, errorMsg };
};");
        }

        return string.Join("\n", code);
    }
}
