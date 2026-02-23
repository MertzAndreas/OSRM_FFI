#include "osrm/engine_config.hpp"
#include "osrm/nearest_parameters.hpp"
#include "osrm/osrm.hpp"
#include "osrm/table_parameters.hpp"
#include <optional>
#include <vector>

static std::vector<std::optional<osrm::engine::Hint>> persistentHints;
static std::vector<osrm::util::Coordinate> persistentCoords;

extern "C" {
osrm::OSRM *InitializeOSRM(const char *basePath) {
  osrm::EngineConfig config{osrm::storage::StorageConfig{basePath}};
  config.use_shared_memory = false;
  return new osrm::OSRM(config);
}

void RegisterStations(osrm::OSRM *osrm, double *coords, int numStations) {
  persistentHints.clear();
  persistentCoords.clear();

  for (int i = 0; i < numStations; ++i) {
    osrm::util::Coordinate c{osrm::util::FloatLongitude{coords[i * 2]},
                             osrm::util::FloatLatitude{coords[i * 2 + 1]}};
    persistentCoords.push_back(c);

    osrm::NearestParameters params;
    params.coordinates.push_back(c);
    osrm::engine::api::ResultT result;

    bool found = false;
    if (osrm->Nearest(params, result) == osrm::Status::Ok) {
      if (auto const *json = std::get_if<osrm::util::json::Object>(&result)) {
        if (json->values.count("waypoints")) {
          if (auto const *waypoints = std::get_if<osrm::util::json::Array>(
                  &json->values.at("waypoints"))) {
            if (!waypoints->values.empty()) {
              if (auto const *wp_obj = std::get_if<osrm::util::json::Object>(
                      &waypoints->values[0])) {
                if (wp_obj->values.count("hint")) {
                  if (auto const *hint_str =
                          std::get_if<osrm::util::json::String>(
                              &wp_obj->values.at("hint"))) {
                    persistentHints.push_back(
                        osrm::engine::Hint::FromBase64(hint_str->value));
                    found = true;
                  }
                }
              }
            }
          }
        }
      }
    }
    // Always push a value to guarantee C# indices match C++ vector memory
    if (!found) {
      persistentHints.push_back(std::nullopt);
    }
  }
}

float *ComputeTableIndexed(osrm::OSRM *osrm, double evLon, double evLat,
                           int *stationIndices, int numIndices, int *outSize) {
  osrm::TableParameters params;
  params.coordinates.push_back(
      {osrm::util::FloatLongitude{evLon}, osrm::util::FloatLatitude{evLat}});
  params.hints.push_back(std::nullopt);

  for (int i = 0; i < numIndices; ++i) {
    int idx = stationIndices[i];
    params.coordinates.push_back(persistentCoords[idx]);
    params.hints.push_back(persistentHints[idx]);
  }

  params.sources = {0};
  params.destinations.resize(numIndices);
  for (int i = 0; i < numIndices; ++i)
    params.destinations[i] = i + 1;

  osrm::engine::api::ResultT result;
  if (osrm->Table(params, result) != osrm::Status::Ok)
    return nullptr;

  auto const *json = std::get_if<osrm::util::json::Object>(&result);
  if (!json || !json->values.count("durations"))
    return nullptr;

  auto const *durations =
      std::get_if<osrm::util::json::Array>(&json->values.at("durations"));
  if (!durations || durations->values.empty())
    return nullptr;

  auto const *row = std::get_if<osrm::util::json::Array>(&durations->values[0]);
  if (!row)
    return nullptr;

  *outSize = row->values.size();
  float *flat = new float[*outSize];

  for (int i = 0; i < *outSize; ++i) {
    auto const *num_ptr =
        std::get_if<osrm::util::json::Number>(&row->values[i]);
    if (num_ptr) {
      flat[i] = (float)num_ptr->value;
    } else {
      flat[i] = -1.0f; // Return -1 for disconnected graph components
    }
  }
  return flat;
}

void FreeMemory(void *ptr) { free(ptr); }
void DeleteOSRM(osrm::OSRM *osrm) { delete osrm; }
}
#include "osrm/engine_config.hpp"
#include "osrm/nearest_parameters.hpp"
#include "osrm/osrm.hpp"
#include "osrm/table_parameters.hpp"
#include <optional>
#include <vector>

    static std::vector < std::optional < osrm::engine::Hint>> persistentHints;
static std::vector<osrm::util::Coordinate> persistentCoords;

extern "C" {
osrm::OSRM *InitializeOSRM(const char *basePath) {
  osrm::EngineConfig config{osrm::storage::StorageConfig{basePath}};
  config.use_shared_memory = false;
  return new osrm::OSRM(config);
}

void RegisterStations(osrm::OSRM *osrm, double *coords, int numStations) {
  persistentHints.clear();
  persistentCoords.clear();

  for (int i = 0; i < numStations; ++i) {
    osrm::util::Coordinate c{osrm::util::FloatLongitude{coords[i * 2]},
                             osrm::util::FloatLatitude{coords[i * 2 + 1]}};
    persistentCoords.push_back(c);

    osrm::NearestParameters params;
    params.coordinates.push_back(c);
    osrm::engine::api::ResultT result;

    bool found = false;
    if (osrm->Nearest(params, result) == osrm::Status::Ok) {
      if (auto const *json = std::get_if<osrm::util::json::Object>(&result)) {
        if (json->values.count("waypoints")) {
          if (auto const *waypoints = std::get_if<osrm::util::json::Array>(
                  &json->values.at("waypoints"))) {
            if (!waypoints->values.empty()) {
              if (auto const *wp_obj = std::get_if<osrm::util::json::Object>(
                      &waypoints->values[0])) {
                if (wp_obj->values.count("hint")) {
                  if (auto const *hint_str =
                          std::get_if<osrm::util::json::String>(
                              &wp_obj->values.at("hint"))) {
                    persistentHints.push_back(
                        osrm::engine::Hint::FromBase64(hint_str->value));
                    found = true;
                  }
                }
              }
            }
          }
        }
      }
    }
    // Always push a value to guarantee C# indices match C++ vector memory
    if (!found) {
      persistentHints.push_back(std::nullopt);
    }
  }
}

float *ComputeTableIndexed(osrm::OSRM *osrm, double evLon, double evLat,
                           int *stationIndices, int numIndices, int *outSize) {
  osrm::TableParameters params;
  params.coordinates.push_back(
      {osrm::util::FloatLongitude{evLon}, osrm::util::FloatLatitude{evLat}});
  params.hints.push_back(std::nullopt);

  for (int i = 0; i < numIndices; ++i) {
    int idx = stationIndices[i];
    params.coordinates.push_back(persistentCoords[idx]);
    params.hints.push_back(persistentHints[idx]);
  }

  params.sources = {0};
  params.destinations.resize(numIndices);
  for (int i = 0; i < numIndices; ++i)
    params.destinations[i] = i + 1;

  osrm::engine::api::ResultT result;
  if (osrm->Table(params, result) != osrm::Status::Ok)
    return nullptr;

  auto const *json = std::get_if<osrm::util::json::Object>(&result);
  if (!json || !json->values.count("durations"))
    return nullptr;

  auto const *durations =
      std::get_if<osrm::util::json::Array>(&json->values.at("durations"));
  if (!durations || durations->values.empty())
    return nullptr;

  auto const *row = std::get_if<osrm::util::json::Array>(&durations->values[0]);
  if (!row)
    return nullptr;

  *outSize = row->values.size();
  float *flat = new float[*outSize];

  for (int i = 0; i < *outSize; ++i) {
    auto const *num_ptr =
        std::get_if<osrm::util::json::Number>(&row->values[i]);
    if (num_ptr) {
      flat[i] = (float)num_ptr->value;
    } else {
      flat[i] = -1.0f; // Return -1 for disconnected graph components
    }
  }
  return flat;
}

void FreeMemory(void *ptr) { free(ptr); }
void DeleteOSRM(osrm::OSRM *osrm) { delete osrm; }
}
#include "osrm/engine_config.hpp"
#include "osrm/nearest_parameters.hpp"
#include "osrm/osrm.hpp"
#include "osrm/table_parameters.hpp"
#include <optional>
#include <vector>

    static std::vector < std::optional < osrm::engine::Hint>> persistentHints;
static std::vector<osrm::util::Coordinate> persistentCoords;

extern "C" {
osrm::OSRM *InitializeOSRM(const char *basePath) {
  osrm::EngineConfig config{osrm::storage::StorageConfig{basePath}};
  config.use_shared_memory = false;
  return new osrm::OSRM(config);
}

void RegisterStations(osrm::OSRM *osrm, double *coords, int numStations) {
  persistentHints.clear();
  persistentCoords.clear();

  for (int i = 0; i < numStations; ++i) {
    osrm::util::Coordinate c{osrm::util::FloatLongitude{coords[i * 2]},
                             osrm::util::FloatLatitude{coords[i * 2 + 1]}};
    persistentCoords.push_back(c);

    osrm::NearestParameters params;
    params.coordinates.push_back(c);
    osrm::engine::api::ResultT result;

    bool found = false;
    if (osrm->Nearest(params, result) == osrm::Status::Ok) {
      if (auto const *json = std::get_if<osrm::util::json::Object>(&result)) {
        if (json->values.count("waypoints")) {
          if (auto const *waypoints = std::get_if<osrm::util::json::Array>(
                  &json->values.at("waypoints"))) {
            if (!waypoints->values.empty()) {
              if (auto const *wp_obj = std::get_if<osrm::util::json::Object>(
                      &waypoints->values[0])) {
                if (wp_obj->values.count("hint")) {
                  if (auto const *hint_str =
                          std::get_if<osrm::util::json::String>(
                              &wp_obj->values.at("hint"))) {
                    persistentHints.push_back(
                        osrm::engine::Hint::FromBase64(hint_str->value));
                    found = true;
                  }
                }
              }
            }
          }
        }
      }
    }
    // Always push a value to guarantee C# indices match C++ vector memory
    if (!found) {
      persistentHints.push_back(std::nullopt);
    }
  }
}

float *ComputeTableIndexed(osrm::OSRM *osrm, double evLon, double evLat,
                           int *stationIndices, int numIndices, int *outSize) {
  osrm::TableParameters params;
  params.coordinates.push_back(
      {osrm::util::FloatLongitude{evLon}, osrm::util::FloatLatitude{evLat}});
  params.hints.push_back(std::nullopt);

  for (int i = 0; i < numIndices; ++i) {
    int idx = stationIndices[i];
    params.coordinates.push_back(persistentCoords[idx]);
    params.hints.push_back(persistentHints[idx]);
  }

  params.sources = {0};
  params.destinations.resize(numIndices);
  for (int i = 0; i < numIndices; ++i)
    params.destinations[i] = i + 1;

  osrm::engine::api::ResultT result;
  if (osrm->Table(params, result) != osrm::Status::Ok)
    return nullptr;

  auto const *json = std::get_if<osrm::util::json::Object>(&result);
  if (!json || !json->values.count("durations"))
    return nullptr;

  auto const *durations =
      std::get_if<osrm::util::json::Array>(&json->values.at("durations"));
  if (!durations || durations->values.empty())
    return nullptr;

  auto const *row = std::get_if<osrm::util::json::Array>(&durations->values[0]);
  if (!row)
    return nullptr;

  *outSize = row->values.size();
  float *flat = new float[*outSize];

  for (int i = 0; i < *outSize; ++i) {
    auto const *num_ptr =
        std::get_if<osrm::util::json::Number>(&row->values[i]);
    if (num_ptr) {
      flat[i] = (float)num_ptr->value;
    } else {
      flat[i] = -1.0f; // Return -1 for disconnected graph components
    }
  }
  return flat;
}

void FreeMemory(void *ptr) { free(ptr); }
void DeleteOSRM(osrm::OSRM *osrm) { delete osrm; }
}
