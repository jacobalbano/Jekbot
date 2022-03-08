use serde::{Deserialize, Serialize};
use ron::de::from_reader;
use ron::ser::{to_string_pretty, PrettyConfig};
use std::fs::File;
use std::io::prelude::*;
use std::path::Path;

#[derive(Debug, Deserialize, Serialize)]
pub struct BotConfig {
    pub token: String
}

impl BotConfig {
	pub fn from_file() -> Self {
		let path = Path::new("config.ron");
		let display = path.display();
		
		let f = match File::open(path) {
			Ok(x) => x,
			Err(_) => {
				let mut file = match File::create(&path) {
					Err(why) => panic!("couldn't create {}: {}", display, why),
					Ok(file) => file,
				};
				
				let empty = BotConfig {
					token: String::from(""),
				};
				
				let pretty = PrettyConfig::new()
					.depth_limit(2)
					.separate_tuple_members(true)
					.enumerate_arrays(true);
					
				match file.write_all(to_string_pretty(&empty, pretty).expect("Serialization failed").as_bytes()) {
					Err(why) => panic!("couldn't write to {}: {}", display, why),
					Ok(_) => panic!("Created empty config file at {}", display),
				}
			},
		};
		
		return match from_reader(f) {
			Ok(x) => x,
			Err(e) => panic!("Failed to load config: {}", e),
		}
	}
}