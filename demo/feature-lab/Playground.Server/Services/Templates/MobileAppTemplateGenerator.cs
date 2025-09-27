using System;

namespace Playground.Server.Services.Templates
{
    public partial class MobileAppTemplateGenerator
    {
        public static string GenerateMobileFitnessCode()
        {
            return @"import React, { useState, useEffect } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, TextInput } from 'react-native';

const FitnessTracker = () => {
  const [workouts, setWorkouts] = useState([]);
  const [newWorkout, setNewWorkout] = useState('');

  const addWorkout = () => {
    if (newWorkout.trim()) {
      setWorkouts([...workouts, { id: Date.now(), name: newWorkout, date: new Date().toISOString() }]);
      setNewWorkout('');
    }
  };

  const deleteWorkout = (id) => {
    setWorkouts(workouts.filter(workout => workout.id !== id));
  };

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>Fitness Tracker</Text>
      
      <View style={styles.inputContainer}>
        <TextInput
          style={styles.input}
          placeholder=""Add a new workout""
          value={newWorkout}
          onChangeText={setNewWorkout}
        />
        <TouchableOpacity style={styles.addButton} onPress={addWorkout}>
          <Text style={styles.buttonText}>Add Workout</Text>
        </TouchableOpacity>
      </View>

      <View style={styles.workoutsList}>
        {workouts.map(workout => (
          <View key={workout.id} style={styles.workoutItem}>
            <Text style={styles.workoutName}>{workout.name}</Text>
            <Text style={styles.workoutDate}>{new Date(workout.date).toLocaleDateString()}</Text>
            <TouchableOpacity 
              style={styles.deleteButton} 
              onPress={() => deleteWorkout(workout.id)}
            >
              <Text style={styles.deleteText}>Delete</Text>
            </TouchableOpacity>
          </View>
        ))}
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
    padding: 20,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: 20,
    color: '#333',
  },
  inputContainer: {
    flexDirection: 'row',
    marginBottom: 20,
  },
  input: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    padding: 12,
    backgroundColor: 'white',
  },
  addButton: {
    backgroundColor: '#007bff',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderRadius: 8,
    marginLeft: 8,
  },
  buttonText: {
    color: 'white',
    fontWeight: 'bold',
  },
  workoutsList: {
    gap: 12,
  },
  workoutItem: {
    backgroundColor: 'white',
    padding: 16,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#ddd',
  },
  workoutName: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
  },
  workoutDate: {
    fontSize: 14,
    color: '#666',
    marginTop: 4,
  },
  deleteButton: {
    backgroundColor: '#dc3545',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 4,
    alignSelf: 'flex-end',
    marginTop: 8,
  },
  deleteText: {
    color: 'white',
    fontSize: 12,
    fontWeight: 'bold',
  },
});

export default FitnessTracker;";
        }
    }
}
