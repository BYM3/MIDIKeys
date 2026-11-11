// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "MIDIKeys",
    platforms: [.macOS(.v14)],
    targets: [
        .target(name: "MIDIKeysCore"),
        .executableTarget(name: "MIDIKeys", dependencies: ["MIDIKeysCore"]),
        .testTarget(name: "MIDIKeysCoreTests", dependencies: ["MIDIKeysCore"]),
    ]
)
