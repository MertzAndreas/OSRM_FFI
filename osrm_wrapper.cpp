#include "osrm/engine_config.hpp"
#include "osrm/nearest_parameters.hpp"
#include "osrm/osrm.hpp"
#include "osrm/table_parameters.hpp"
#include <cstdlib>
#include <optional>
#include <vector>

static std::vector<std::optional<osrm::engine::Hint>> persistentHints;
static std::vector<osrm::util::Coordinate> persistentCoords;

extern "C" {
osrm::OSRM *InitializeOSRM(const char *basePath) {
  osrm::EngineConfig config;
  config.storage_config = osrm::storage::StorageConfig(basePath);
  config.use_shared_memory = false;
  return new osrm::OSRM(config);
}

void RegisterStations(osrm::OSRM *osrm, double *coords, int numStations) {
  persistentHints.clear();
  persistentCoords.clear();

  for (int i = 0; i < numStations; ++i) {
    osrm::util::Coordinate c{osrm::util::FloatLongitude{coords[i * 2]},
                             osrm::util::FloatLatitude{coords[i * 2 + 1]}};

    osrm::NearestParameters params;
    params.coordinates.push_back(c);
    params.radiuses.push_back(100.0);

    osrm::engine::api::ResultT result;
    bool found = false;

    if (osrm->Nearest(params, result) == osrm::Status::Ok) {
      try {
        auto const &json = std::get<osrm::util::json::Object>(result);
        auto const &waypoints =
            std::get<osrm::util::json::Array>(json.values.at("waypoints"));
        auto const &wp_obj =
            std::get<osrm::util::json::Object>(waypoints.values.at(0));

        auto const &hint_str =
            std::get<osrm::util::json::String>(wp_obj.values.at("hint"));
        auto const &loc_arr =
            std::get<osrm::util::json::Array>(wp_obj.values.at("location"));

        double lon =
            std::get<osrm::util::json::Number>(loc_arr.values.at(0)).value;
        double lat =
            std::get<osrm::util::json::Number>(loc_arr.values.at(1)).value;

        persistentHints.push_back(
            osrm::engine::Hint::FromBase64(hint_str.value));
        persistentCoords.push_back(
            {osrm::util::FloatLongitude{lon}, osrm::util::FloatLatitude{lat}});
        found = true;
      } catch (const std::exception &) {
        // Fallthrough on missing keys or variant mismatch
      }
    }

    if (!found) {
      persistentHints.push_back(std::nullopt);
      persistentCoords.push_back(c);
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
  for (int i = 0; i < numIndices; ++i) {
    params.destinations[i] = i + 1;
  }

  osrm::engine::api::ResultT result;
  if (osrm->Table(params, result) != osrm::Status::Ok) {
    return nullptr;
  }

  try {
    auto const &json = std::get<osrm::util::json::Object>(result);
    auto const &durations =
        std::get<osrm::util::json::Array>(json.values.at("durations"));
    auto const &row = std::get<osrm::util::json::Array>(durations.values.at(0));

    *outSize = row.values.size();
    float *flat = (float *)malloc(sizeof(float) * (*outSize));

    for (int i = 0; i < *outSize; ++i) {
      if (auto const *num_ptr =
              std::get_if<osrm::util::json::Number>(&row.values[i])) {
        flat[i] = static_cast<float>(num_ptr->value);
      } else {
        flat[i] = -1.0f;
      }
    }
    return flat;
  } catch (const std::exception &) {
    return nullptr;
  }
}

void FreeMemory(void *ptr) { free(ptr); }
void DeleteOSRM(osrm::OSRM *osrm) { delete osrm; }
}
