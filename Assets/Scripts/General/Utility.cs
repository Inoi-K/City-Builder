public static class Utility {
    // The Fisher–Yates shuffle
    public static T[] ShuffleArray<T>(T[] array, int seed) {
        System.Random prng = new System.Random(seed);

        for (int i = 0; i < array.Length - 1; ++i) {
            int randomIndex = prng.Next(i, array.Length);
            (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
        }

        return array;
    }
}
