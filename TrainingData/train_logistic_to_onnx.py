import pandas as pd
import numpy as np

from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline
from sklearn.metrics import classification_report, accuracy_score

from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType

# 1. Load Dataset

data_path = "complete_training_data.csv"
df = pd.read_csv(data_path)

# 2. Generate Label

def generate_label(row):

    # Hard
    if row["avg_time_per_enemy"] > 10:
        return 2

    elif row["avg_time_per_enemy"] > 8 and row["accuracy"] < 0.65:
        return 2

    # Easy
    elif row["avg_time_per_enemy"] < 4:
        return 0

    # Balanced
    else:
        return 1

df["label"] = df.apply(generate_label, axis=1)

print("Label Distribution:")
print(df["label"].value_counts())
print("-" * 40)

# 3. Define Features
feature_columns = [
    "wpm",
    "accuracy",
    "mistake_count",
    "reaction_time_avg",
    "avg_time_per_enemy"
]

X = df[feature_columns]
y = df["label"]

# 4. Train/Test Split
X_train, X_test, y_train, y_test = train_test_split(
    X, y,
    test_size=0.2,
    random_state=42,
    stratify=y
)

# 5. Build Pipeline
pipeline = Pipeline([
    ("scaler", StandardScaler()),
    ("logreg", LogisticRegression(
        max_iter=2000,
        solver="lbfgs",
        class_weight="balanced",
        random_state=42
    ))
])

# 6. Train Model
pipeline.fit(X_train, y_train)

# 7. Evaluate Model
y_pred = pipeline.predict(X_test)

print("Accuracy:", accuracy_score(y_test, y_pred))
print("\nClassification Report:")
print(classification_report(y_test, y_pred))
print("-" * 40)

print("Classes learned:", pipeline.named_steps["logreg"].classes_)

# 8. Export to ONNX
initial_type = [
    ("float_input", FloatTensorType([None, len(feature_columns)]))
]

onnx_model = convert_sklearn(
    pipeline,
    initial_types=initial_type
)

onnx_filename = "logistic_model.onnx"

with open(onnx_filename, "wb") as f:
    f.write(onnx_model.SerializeToString())

print(f"Model exported as {onnx_filename}")
